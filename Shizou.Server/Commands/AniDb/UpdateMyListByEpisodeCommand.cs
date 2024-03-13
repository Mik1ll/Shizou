using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class UpdateMyListByEpisodeCommand : Command<UpdateMyListByEpisodeArgs>
{
    private readonly ILogger<UpdateMyListByEpisodeCommand> _logger;
    private readonly IShizouContext _context;
    private readonly IMyListAddRequest _myListAddRequest;
    private readonly IMyListEntryRequest _myListEntryRequest;
    private readonly CommandService _commandService;
    private readonly ShizouOptions _options;

    public UpdateMyListByEpisodeCommand(
        ILogger<UpdateMyListByEpisodeCommand> logger,
        IShizouContext context,
        IMyListAddRequest myListAddRequest,
        IMyListEntryRequest myListEntryRequest,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot)
    {
        _options = optionsSnapshot.Value;
        _logger = logger;
        _context = context;
        _myListAddRequest = myListAddRequest;
        _myListEntryRequest = myListEntryRequest;
        _commandService = commandService;
    }

    protected override async Task ProcessInnerAsync()
    {
        _myListAddRequest.SetParameters(CommandArgs.Aid, CommandArgs.EpNo, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate,
            CommandArgs.MyListState);
        _logger.LogInformation(
            "Sending {RequestType} mylist request: AnimeId = {Aid}, EpNo = {EpNo}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}",
            CommandArgs.Edit ? "EDIT" : "ADD", CommandArgs.Aid, CommandArgs.EpNo, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
        var response = await _myListAddRequest.ProcessAsync().ConfigureAwait(false);
        switch (response?.ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (CommandArgs.EpNo != "0" && !CommandArgs.EpNo.StartsWith("-"))
                    await GetMyListEntryAsync(response).ConfigureAwait(false);
                break;
            case AniDbResponseCode.FileInMyList:
                break;
            case AniDbResponseCode.MultipleMyListEntries:
                break;
            case AniDbResponseCode.MyListEdited:
                break;
            case AniDbResponseCode.NoSuchMyListEntry:
                break;
            case null:
                _logger.LogWarning("Did not recieve response");
                return;
            default:
                _logger.LogWarning("Unexpected response code {ResponseCode}", response.ResponseCode);
                return;
        }

        Completed = true;
    }

    private async Task GetMyListEntryAsync(MyListAddResponse addResponse)
    {
        _myListEntryRequest.SetParameters(CommandArgs.Aid, CommandArgs.EpNo);
        var entryResponse = await _myListEntryRequest.ProcessAsync().ConfigureAwait(false);
        switch (entryResponse?.ResponseCode)
        {
            case AniDbResponseCode.MyList:
                if (entryResponse.MyListEntryResult is { } entryResult)
                {
                    if (_context.FileWatchedStates.FirstOrDefault(ws => ws.AniDbFileId == entryResult.FileId) is { } eWs)
                    {
                        _logger.LogInformation("Updating episode id {EpisodeId} with generic file id {GenericId} and mylist id {MyListId}",
                            entryResult.EpisodeId, entryResult.FileId, entryResult.MyListId);
                        eWs.AniDbFileId = entryResult.FileId;
                        eWs.MyListId = entryResult.MyListId;
                        _context.SaveChanges();
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Adding watch state for episode id {EpisodeId} with generic file id {GenericId} and mylist id {MyListId}",
                            entryResult.EpisodeId, entryResult.FileId, entryResult.MyListId);
                        if (!_context.AniDbFiles.Any(f => f.Id == entryResult.FileId))
                            _context.AniDbGenericFiles.Add(new AniDbGenericFile
                            {
                                Id = entryResult.FileId,
                                AniDbEpisodeFileXrefs =
                                    [new AniDbEpisodeFileXref { AniDbFileId = entryResult.FileId, AniDbEpisodeId = entryResult.EpisodeId }]
                            });
                        _context.FileWatchedStates.Add(new FileWatchedState
                        {
                            AniDbFileId = entryResult.FileId,
                            Watched = CommandArgs.Watched ?? entryResult.ViewDate is not null,
                            WatchedUpdated = CommandArgs.Watched is null ? entryResult.ViewDate?.UtcDateTime : DateTime.UtcNow,
                            MyListId = entryResult.MyListId
                        });
                        _context.SaveChanges();
                    }

                    ManuallyLinkFile(entryResult);
                    if (addResponse.EntriesAffected == 0)
                        _commandService.Dispatch(new UpdateMyListArgs(true, _options.AniDb.MyList.PresentFileState, CommandArgs.Watched,
                            CommandArgs.WatchedDate, entryResult.MyListId));
                }
                else
                {
                    throw new AniDbUdpRequestException("UDP MYLIST entry result should not be null");
                }

                break;
            case AniDbResponseCode.MultipleMyListEntries:
                _logger.LogWarning(
                    "Could not get generic file mylist entry for anime id {AnimeId} episode {EpisodeNumber}, multiple entries returned",
                    CommandArgs.Aid, CommandArgs.EpNo);
                break;
            case AniDbResponseCode.NoSuchEntry:
                _logger.LogError("Could not get generic file mylist entry for anime id {AnimeId} episode {EpisodeNumber}, no file returned",
                    CommandArgs.Aid, CommandArgs.EpNo);
                break;
            default:
                throw new AniDbUdpRequestException(
                    $"Unexpected response for UDP MYLIST Code:{entryResponse?.ResponseCode}, Text:\"{entryResponse?.ResponseText}\"");
        }
    }

    private void ManuallyLinkFile(MyListEntryResult entryResult)
    {
        if (CommandArgs.ManualLinkToLocalFileId is { } localFileId)
        {
            var localFile = _context.LocalFiles.FirstOrDefault(lf => lf.Id == localFileId);
            if (localFile is not null)
            {
                localFile.AniDbFileId = entryResult.FileId;
                _context.SaveChanges();
                _logger.LogInformation("Local file id {LocalFileId} with name \"{LocalFileName}\" manually linked to episode id {EpisodeId}", localFileId,
                    Path.GetFileName(localFile.PathTail), entryResult.EpisodeId);
            }
            else
            {
                _logger.LogWarning("Local file entry for id {LocalFileId} not found, cannot manually link", localFileId);
            }
        }
    }
}
