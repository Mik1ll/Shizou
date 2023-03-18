using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests.Udp;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Enums;

namespace Shizou.Commands.AniDb;

public record UpdateMyListParams(
        int? Lid,
        int? Fid,
        int? Aid, string? EpNo,
        bool? Edit,
        bool? Watched,
        DateTimeOffset? WatchedDate,
        MyListState? MyListState,
        MyListFileState? MyListFileState
    )
    : CommandParams($"{nameof(UpdateMyListCommand)}_lid={Lid}_fid={Fid}_aid={Aid}_epno={EpNo}"
                    + $"_edit={Edit}_watched={Watched}_state={MyListState}_filestate={MyListFileState}");

[Command(CommandType.UpdateMyList, CommandPriority.Default, QueueType.AniDbUdp)]
public class UpdateMyListCommand : BaseCommand<UpdateMyListParams>
{
    private readonly ShizouContext _context;

    public UpdateMyListCommand(IServiceProvider provider, UpdateMyListParams commandParams) : base(provider, commandParams)
    {
        _context = provider.GetRequiredService<ShizouContext>();
    }

    public override async Task Process()
    {
        bool retry;
        do
        {
            retry = false;
            var request = CommandParams switch
            {
                { Lid: not null, Edit: true } and ({ Fid: not null } or { Aid: not null, EpNo: not null }) =>
                    new MyListAddRequest(Provider, CommandParams.Lid.Value, CommandParams.Watched, CommandParams.WatchedDate, CommandParams.MyListState,
                        CommandParams.MyListFileState),
                { Fid: not null, Edit: not null } =>
                    new MyListAddRequest(Provider, CommandParams.Fid.Value, CommandParams.Edit.Value, CommandParams.Watched, CommandParams.WatchedDate,
                        CommandParams.MyListState, CommandParams.MyListFileState),
                { Aid: not null, EpNo: not null, Edit: not null } =>
                    new MyListAddRequest(Provider, CommandParams.Aid.Value, CommandParams.EpNo, CommandParams.Edit.Value, CommandParams.Watched,
                        CommandParams.WatchedDate, CommandParams.MyListState, CommandParams.MyListFileState),
                _ => null
            };
            if (request is null)
            {
                Completed = true;
                Logger.LogError("Skipping, arguments are not valid");
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
                    CommandParams = CommandParams with { Lid = null, Edit = false };
                    retry = true;
                    Logger.LogInformation("Mylist entry not found, retrying with add");
                    break;
            }
        } while (retry);
        Completed = true;
    }
}
