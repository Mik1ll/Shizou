using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests;
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
        MyListFileState? MyListFileState)
    : CommandParams(nameof(UpdateMyListCommand)
                    + (Lid is null ? "" : "_lid=" + Lid)
                    + (Fid is null ? "" : "_fid=" + Fid)
                    + (Aid is null ? "" : $"_aid={Aid}_epno={EpNo}")
                    + $"_edit={Edit}_watched={Watched}_state={MyListState}_filestate={MyListFileState}");

[Command(CommandType.UpdateMyList, CommandPriority.Default, QueueType.AniDbUdp)]
public class UpdateMyListCommand : BaseCommand<UpdateMyListParams>
{
    private readonly ShizouContext _context;

    public UpdateMyListCommand(IServiceProvider provider, UpdateMyListParams commandParams) : base(provider, commandParams)
    {
        _context = provider.GetRequiredService<ShizouContext>();
    }

    public override Task Process()
    {
        if (new[] { CommandParams.Lid, CommandParams.Fid, CommandParams.Aid }.Count(e => e is not null) > 1)
        {
            Completed = true;
            Logger.LogError($"Skipping, Only one of Lid, Fid, and Aid can be used in {nameof(UpdateMyListCommand)}");
        }

        if (CommandParams.Lid is not null)
        {
            var request = new MyListAddRequest(Provider, CommandParams.Lid.Value, CommandParams.Watched, CommandParams.WatchedDate,
                CommandParams.MyListState, CommandParams.MyListFileState);
        }
        else if (CommandParams.Fid is not null && CommandParams.Edit is not null)
        {
            var request = new MyListAddRequest(Provider, CommandParams.Fid.Value, CommandParams.Edit.Value, CommandParams.Watched,
                CommandParams.WatchedDate, CommandParams.MyListState, CommandParams.MyListFileState);
        }
        else if (CommandParams.Aid is not null && CommandParams.EpNo is not null && CommandParams.Edit is not null)
        {
            var request = new MyListAddRequest(Provider, CommandParams.Aid.Value, CommandParams.EpNo, CommandParams.Edit.Value,
                CommandParams.Watched, CommandParams.WatchedDate, CommandParams.MyListState, CommandParams.MyListFileState);
        }
        else
        {
            Completed = true;
            Logger.LogError($"Skipping, One or more required parameters are missing in {nameof(UpdateMyListCommand)}");
        }


        return Task.CompletedTask;
    }
}
