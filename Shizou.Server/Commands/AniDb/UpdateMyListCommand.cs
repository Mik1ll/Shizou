using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;

namespace Shizou.Server.Commands.AniDb;

public class UpdateMyListCommand : Command<UpdateMyListArgs>
{
    private readonly IShizouContext _context;
    private readonly ILogger<UpdateMyListCommand> _logger;
    private readonly IMyListAddRequest _myListAddRequest;

    public UpdateMyListCommand(
        ILogger<UpdateMyListCommand> logger,
        IShizouContext context,
        IMyListAddRequest myListAddRequest
    )
    {
        _logger = logger;
        _context = context;
        _myListAddRequest = myListAddRequest;
    }

    protected override async Task ProcessInnerAsync()
    {
        switch (CommandArgs)
        {
            case { Lid: not null, Fid: null, Edit: true }:
                _myListAddRequest.SetParameters(CommandArgs.Lid.Value, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                _logger.LogInformation(
                    "Sending EDIT mylist request: MyListId = {Lid}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}",
                    CommandArgs.Lid, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                break;
            case { Fid: not null, Lid: null, Edit: false }:
                _myListAddRequest.SetParameters(CommandArgs.Fid.Value, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                _logger.LogInformation(
                    "Sending ADD mylist request: FileId = {Fid}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}",
                    CommandArgs.Fid, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                break;
            default: throw new ArgumentException($"{nameof(UpdateMyListArgs)} arguments not valid");
        }


        var response = await _myListAddRequest.ProcessAsync().ConfigureAwait(false);
        switch (response?.ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (response.AddedEntryId is not null)
                    SaveMyListId(CommandArgs.Fid!.Value, response.AddedEntryId.Value);
                else
                    throw new AniDbUdpRequestException("Added entry id should not be null");
                break;
            case AniDbResponseCode.FileInMyList:
                if (response.ExistingEntryResult is not null)
                    SaveMyListId(response.ExistingEntryResult.FileId, response.ExistingEntryResult.MyListId);
                else
                    throw new AniDbUdpRequestException("Existing entry result should not be null");
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
        if (_context.FileWatchedStates.FirstOrDefault(ws => ws.AniDbFileId == fileId) is { } fileWatchedState)
            fileWatchedState.MyListId = myListId;

        _context.SaveChanges();
    }
}
