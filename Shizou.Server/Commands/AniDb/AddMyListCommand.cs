using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;

namespace Shizou.Server.Commands.AniDb;

public class AddMyListCommand : Command<AddMyListArgs>
{
    private readonly ILogger<AddMyListCommand> _logger;
    private readonly IShizouContext _context;
    private readonly IMyListAddRequest _myListAddRequest;

    public AddMyListCommand(
        ILogger<AddMyListCommand> logger,
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
        _myListAddRequest.SetParameters(CommandArgs.Fid, false, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
        _logger.LogInformation(
            "Sending ADD mylist request: FileId = {Fid}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}",
            CommandArgs.Fid, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);


        var response = await _myListAddRequest.ProcessAsync().ConfigureAwait(false);
        switch (response?.ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (response.AddedEntryId is not null)
                    SaveMyListId(CommandArgs.Fid, response.AddedEntryId.Value);
                else
                    throw new AniDbUdpRequestException("Added entry id should not be null");
                break;
            case AniDbResponseCode.FileInMyList:
                if (response.ExistingEntryResult is not null)
                    SaveMyListId(response.ExistingEntryResult.FileId, response.ExistingEntryResult.MyListId);
                else
                    throw new AniDbUdpRequestException("Existing entry result should not be null");
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
