using System.Text.Json;

namespace Shizou.Commands
{
    [Command(CommandType.Noop, CommandPriority.Default, QueueType.General)]
    public sealed class NoopCommand : BaseCommand
    {
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

        protected override void PopulateCommandParams(string commandParams)
        {
        }
    }
}
