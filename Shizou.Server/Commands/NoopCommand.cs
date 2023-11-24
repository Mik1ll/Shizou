using System.Threading.Tasks;
using Shizou.Data.CommandArgs;

namespace Shizou.Server.Commands;

public sealed class NoopCommand : Command<NoopArgs>
{
    protected override async Task ProcessInnerAsync()
    {
        await Task.Delay(10_000).ConfigureAwait(false);
        Completed = true;
    }
}
