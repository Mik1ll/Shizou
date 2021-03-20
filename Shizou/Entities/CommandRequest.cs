using Shizou.Commands;

namespace Shizou.Entities
{
    public class CommandRequest : Entity
    {
        CommandType Type { get; }
        
        public CommandPriority Priority { get; } = CommandPriority.Default;

        public string CommandId { get; } = null!;

        public string CommandParams { get; } = null!;
    }
}
