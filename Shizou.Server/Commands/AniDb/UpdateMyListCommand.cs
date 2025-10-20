using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.Commands.AniDb;

public class UpdateMyListCommand : Command<UpdateMyListArgs>
{
    private readonly ILogger<UpdateMyListCommand> _logger;
    private readonly IMyListAddRequest _myListAddRequest;

    public UpdateMyListCommand(
        ILogger<UpdateMyListCommand> logger,
        IMyListAddRequest myListAddRequest
    )
    {
        _logger = logger;
        _myListAddRequest = myListAddRequest;
    }

    protected override async Task ProcessInnerAsync()
    {
        _myListAddRequest.SetParameters(CommandArgs.Lid, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
        _logger.LogInformation(
            "Sending EDIT mylist request: MyListId = {Lid}, Watched = {Watched}, WatchedDate = {WatchedDate}, MyListState = {MyListState}, FileId = {FileId}",
            CommandArgs.Lid, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState, CommandArgs.FileId);
        var response = await _myListAddRequest.ProcessAsync().ConfigureAwait(false);
        switch (response?.ResponseCode)
        {
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
}
