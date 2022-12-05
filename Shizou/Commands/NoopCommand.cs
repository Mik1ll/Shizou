using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;

namespace Shizou.Commands;

public sealed record NoopParams(int Testint) : CommandParams(nameof(NoopCommand) + Testint);

[Command(CommandType.Noop, CommandPriority.Default, QueueType.AniDbUdp)]
public sealed class NoopCommand : BaseCommand<NoopParams>
{
    public NoopCommand(IServiceProvider provider, NoopParams commandParams)
        : base(provider, provider.GetRequiredService<ILogger<NoopCommand>>(), commandParams)
    {
    }

    public override Task Process()
    {
        Completed = true;
        return Task.CompletedTask;
    }
}