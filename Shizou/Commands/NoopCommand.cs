using System.Text.Json;

namespace Shizou.Commands
{
    public sealed class NoopCommand : BaseCommand
    {
        public NoopCommand() : base(CommandType.Noop, CommandPriority.Default, QueueType.General)
        {
        }

        public override void Process()
        {
        }

        protected override string GenerateCommandId()
        {
            return nameof(NoopCommand);
        }

        protected override string GenerateCommandParams()
        {
            return JsonSerializer.Serialize(new { });
        }

        protected override void ParamsFromCommandRequest()
        {
        }
    }
}
