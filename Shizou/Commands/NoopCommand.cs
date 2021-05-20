using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;

namespace Shizou.Commands
{
    public sealed record NoopParams : CommandParams
    {
        public int Testint { get; set; }
    }

    [Command(CommandType.Noop, CommandPriority.Default, QueueType.General)]
    public sealed class NoopCommand : BaseCommand<NoopParams>
    {
        public NoopCommand(NoopParams commandParams, ILogger<NoopCommand> logger) : base(commandParams, logger)
        {
        }

        public override string CommandId => nameof(NoopCommand) + CommandParams.Testint;

        public override async Task Process()
        {
            Completed = true;
        }
    }
}
