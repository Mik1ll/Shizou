using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;

namespace Shizou.Server.Commands.AniDb;

public class UpdateMyListCommand : Command<UpdateMyListArgs>
{
    private readonly IShizouContext _context;
    private readonly ILogger<UpdateMyListCommand> _logger;
    private readonly IMyListAddRequest _myListAddRequest;
    private readonly IMyListEntryRequest _myListEntryRequest;

    public UpdateMyListCommand(
        ILogger<UpdateMyListCommand> logger,
        IShizouContext context,
        IMyListAddRequest myListAddRequest,
        IMyListEntryRequest myListEntryRequest
    )
    {
        _logger = logger;
        _context = context;
        _myListAddRequest = myListAddRequest;
        _myListEntryRequest = myListEntryRequest;
    }

    protected override async Task ProcessInnerAsync()
    {
        switch (CommandArgs)
        {
            case { Lid: not null, Fid: null, Aid: null, EpNo: null, Edit: true }:
                _myListAddRequest.SetParameters(CommandArgs.Lid.Value, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                _logger.LogInformation(
                    "Sending EDIT mylist request: MyListId = {Lid}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}",
                    CommandArgs.Lid, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                break;
            case { Fid: not null, Lid: null, Aid: null, EpNo: null, Edit: false }:
                _myListAddRequest.SetParameters(CommandArgs.Fid.Value, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                _logger.LogInformation(
                    "Sending ADD mylist request: FileId = {Fid}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}",
                    CommandArgs.Fid, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                break;
            case { Aid: not null, EpNo: not null, Lid: null, Fid: null }:
                _myListAddRequest.SetParameters(CommandArgs.Aid.Value, CommandArgs.EpNo, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate,
                    CommandArgs.MyListState);
                _logger.LogInformation(
                    "Sending {RequestType} mylist request: AnimeId = {Aid}, EpNo = {EpNo}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}",
                    CommandArgs.Edit ? "EDIT" : "ADD", CommandArgs.Aid, CommandArgs.EpNo, CommandArgs.Watched, CommandArgs.WatchedDate,
                    CommandArgs.MyListState);
                break;
            default: throw new ArgumentException($"{nameof(UpdateMyListArgs)} not valid");
        }


        var response = await _myListAddRequest.ProcessAsync().ConfigureAwait(false);
        switch (response?.ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (CommandArgs is { Aid: { } aid, EpNo: { } epno } && epno != "0" && !epno.StartsWith("-"))
                {
                    _myListEntryRequest.SetParameters(aid, epno);
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
                            }
                            else
                            {
                                throw new AniDbUdpRequestException("UDP MYLIST entry result should not be null");
                            }

                            break;
                        case AniDbResponseCode.MultipleMyListEntries:
                            _logger.LogWarning(
                                "Could not get generic file mylist entry for anime id {AnimeId} episode {EpisodeNumber}, multiple entries returned", aid, epno);
                            break;
                        case AniDbResponseCode.NoSuchEntry:
                            _logger.LogError("Could not get generic file mylist entry for anime id {AnimeId} episode {EpisodeNumber}, no file returned", aid,
                                epno);
                            break;
                        default:
                            throw new AniDbUdpRequestException(
                                $"Unexpected response for UDP MYLIST Code:{entryResponse?.ResponseCode}, Text:\"{entryResponse?.ResponseText}\"");
                    }
                }
                else if (response.AddedEntryId is not null && CommandArgs.Fid is not null)
                {
                    SaveMyListId(CommandArgs.Fid.Value, response.AddedEntryId.Value);
                }

                break;
            case AniDbResponseCode.FileInMyList:
                if (CommandArgs is { Fid: not null })
                    SaveMyListId(response.ExistingEntryResult!.FileId, response.ExistingEntryResult!.MyListId);
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

    private void SaveMyListId(int fileId, int myListId)
    {
        if (_context.FileWatchedStates.Find(fileId) is { } fileWatchedState)
            fileWatchedState.MyListId = myListId;

        _context.SaveChanges();
    }
}
