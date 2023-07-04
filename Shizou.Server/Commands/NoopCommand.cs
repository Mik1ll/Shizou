using System.Threading.Tasks;
using Shizou.Data.Enums;

namespace Shizou.Server.Commands;

public sealed record NoopArgs(int Testint) : CommandArgs($"{nameof(NoopCommand)}_{Testint}");

[Command(CommandType.Noop, CommandPriority.Normal, QueueType.General)]
public sealed class NoopCommand : BaseCommand<NoopArgs>
{
    protected override async Task ProcessInner()
    {
        await Task.Delay(10_000);
        Completed = true;
    }
}
