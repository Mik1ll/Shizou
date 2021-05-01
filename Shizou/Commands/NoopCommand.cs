using System.Text.Json;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Commands
{
    public sealed record NoopParams : CommandParams
    {
        public int Testint { get; set; }
    }
    
    [Command(CommandType.Noop, CommandPriority.Default, QueueType.General)]
    public sealed class NoopCommand : BaseCommand
    {
        protected override NoopParams CommandParams => (NoopParams)_commandParams;

        public NoopCommand(NoopParams commandParams) : base(commandParams)
        {
        }

        public NoopCommand(CommandRequest commandRequest) : base(commandRequest, typeof(NoopParams))
        {
        }

        public override void Process()
        {
        }

        protected override string GenerateCommandId()
        {
            return nameof(NoopCommand);
        }
    }
}
