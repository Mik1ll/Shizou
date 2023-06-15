using System;
using System.Threading.Tasks;
using Shizou.Common.Enums;

namespace Shizou.Server.Commands;

public sealed record NoopArgs(int Testint) : CommandArgs(nameof(NoopCommand) + Testint);

[Command(CommandType.Noop, CommandPriority.Normal, QueueType.General)]
public sealed class NoopCommand : BaseCommand<NoopArgs>
{
    public NoopCommand(IServiceProvider provider, NoopArgs commandArgs) : base(provider, commandArgs)
    {
    }

    public override async Task Process()
    {
        await Task.Delay(10_000);
        Completed = true;
    }
}
