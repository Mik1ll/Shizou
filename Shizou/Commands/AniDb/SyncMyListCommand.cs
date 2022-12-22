using System;
using System.Threading.Tasks;
using Shizou.CommandProcessors;

namespace Shizou.Commands.AniDb;

public sealed record SyncMyListParams() : CommandParams($"{nameof(SyncMyListCommand)}");

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
