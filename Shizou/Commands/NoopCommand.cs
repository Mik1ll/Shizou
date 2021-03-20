using System.Text.Json;
using Shizou.Entities;

namespace Shizou.Commands
{
    public sealed class NoopCommand : BaseCommand
    {
        public NoopCommand() : base(new CommandRequest())
        {
            CommandRequest.CommandId = GenerateCommandId();
            CommandRequest.Type = CommandType.Noop;
            CommandRequest.Priority = CommandPriority.Default;
            CommandRequest.CommandParams = JsonSerializer.Serialize(new {});
        }
        
        public NoopCommand(CommandRequest commandRequest) : base(commandRequest)
        {
        }

        public override void Process()
        {
        }

        protected override string GenerateCommandId()
        {
            return nameof(NoopCommand);
        }

        protected override void ParamsFromCommandRequest()
        {
        }
    }
}
