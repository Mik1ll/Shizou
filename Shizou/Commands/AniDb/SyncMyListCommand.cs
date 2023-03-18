using System;
using System.Threading.Tasks;
using Shizou.CommandProcessors;

namespace Shizou.Commands.AniDb;

public sealed record SyncMyListParams(bool ForceRefresh = false) : CommandParams($"{nameof(SyncMyListCommand)}_force={ForceRefresh}");

[Command(CommandType.SyncMyList, CommandPriority.Default, QueueType.AniDbUdp)]
public class SyncMyListCommand : BaseCommand<SyncMyListParams>
{
    public SyncMyListCommand(IServiceProvider provider, SyncMyListParams commandParams) : base(provider, commandParams)
    {
    }

    public override Task Process()
    {
        throw new NotImplementedException();
    }
}
