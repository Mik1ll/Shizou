using System.Threading.Tasks;
using Shizou.Data.Enums;

namespace Shizou.Server.Commands;

[Command(typeof(NoopCommand), CommandPriority.Normal, QueueType.General)]
public sealed record NoopArgs(int Testint) : CommandArgs($"{nameof(NoopCommand)}_{Testint}");

public sealed class NoopCommand : Command<NoopArgs>
{
    protected override async Task ProcessInnerAsync()
    {
        await Task.Delay(10_000).ConfigureAwait(false);
        Completed = true;
    }
}
