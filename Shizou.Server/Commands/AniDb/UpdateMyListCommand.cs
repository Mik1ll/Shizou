using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

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
                    if (entryResponse?.MyListEntryResult is { } entryResult)
                        if (_context.EpisodeWatchedStates.FirstOrDefault(ws => ws.AniDbEpisodeId == entryResult.EpisodeId) is { } eWs)
                        {
                            _logger.LogInformation("Updating episode {EpisodeId} with generic file id {GenericId} and mylist id {MyListId}",
                                entryResult.EpisodeId, entryResult.FileId, entryResult.MyListId);
                            eWs.AniDbFileId = entryResult.FileId;
                            eWs.MyListId = entryResult.MyListId;
                            _context.SaveChanges();
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
        else if (_context.EpisodeWatchedStates.FirstOrDefault(ws => ws.AniDbFileId == fileId) is { } epWatchedState)
            epWatchedState.MyListId = myListId;

        _context.SaveChanges();
    }
}
