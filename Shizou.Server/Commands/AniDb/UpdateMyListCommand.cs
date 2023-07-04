using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Udp;

namespace Shizou.Server.Commands.AniDb;

public record UpdateMyListArgs(
        int? Lid = null,
        int? Fid = null,
        int? Aid = null, string? EpNo = null,
        bool? Edit = null,
        bool? Watched = null,
        DateTimeOffset? WatchedDate = null,
        MyListState? MyListState = null,
        MyListFileState? MyListFileState = null
    )
    : CommandArgs($"{nameof(UpdateMyListCommand)}_lid={Lid}_fid={Fid}_aid={Aid}_epno={EpNo}"
                  + $"_edit={Edit}_watched={Watched}_state={MyListState}_filestate={MyListFileState}");

[Command(CommandType.UpdateMyList, CommandPriority.Normal, QueueType.AniDbUdp)]
public class UpdateMyListCommand : BaseCommand<UpdateMyListArgs>
{
    private readonly ILogger<UpdateMyListCommand> _logger;
    private readonly ShizouContext _context;
    private readonly UdpRequestFactory _udpRequestFactory;

    public UpdateMyListCommand(
        ILogger<UpdateMyListCommand> logger,
        ShizouContext context,
        UdpRequestFactory udpRequestFactory
    )
    {
        _logger = logger;
        _context = context;
        _udpRequestFactory = udpRequestFactory;
    }

    protected override async Task ProcessInner()
    {
        bool retry;
        do
        {
            retry = false;
            var request = CommandArgs switch
            {
                { Lid: not null, Edit: true } and ({ Fid: not null } or { Aid: not null, EpNo: not null }) =>
                    _udpRequestFactory.MyListAddRequest(CommandArgs.Lid.Value, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState,
                        CommandArgs.MyListFileState),
                { Fid: not null, Edit: not null } =>
                    _udpRequestFactory.MyListAddRequest(CommandArgs.Fid.Value, CommandArgs.Edit.Value, CommandArgs.Watched, CommandArgs.WatchedDate,
                        CommandArgs.MyListState, CommandArgs.MyListFileState),
                { Aid: not null, EpNo: not null, Edit: not null } =>
                    _udpRequestFactory.MyListAddRequest(CommandArgs.Aid.Value, CommandArgs.EpNo, CommandArgs.Edit.Value, CommandArgs.Watched,
                        CommandArgs.WatchedDate, CommandArgs.MyListState, CommandArgs.MyListFileState),
                _ => null
            };
            if (request is null)
            {
                Completed = true;
                _logger.LogError("Skipping, arguments are not valid");
                return;
            }
            await request.Process();
            switch (request.ResponseCode)
            {
                case AniDbResponseCode.MyListAdded:
                    // check if less than number of episodes on aid add and run again with edit
                    break;
                case AniDbResponseCode.FileInMyList:
                    break;
                case AniDbResponseCode.MultipleMyListEntries:
                    break;
                case AniDbResponseCode.MyListEdited:
                    // check if less than number of episodes on aid edit and run again with add
                    break;

                case AniDbResponseCode.NoSuchMyListEntry:
                    CommandArgs = CommandArgs with { Lid = null, Edit = false };
                    retry = true;
                    _logger.LogInformation("Mylist entry not found, retrying with add");
                    break;
            }
        } while (retry);
        Completed = true;
    }
}
