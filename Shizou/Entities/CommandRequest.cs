using Shizou.Commands;

namespace Shizou.Entities
{
    public class CommandRequest : Entity
    {
        public CommandType Type { get; }
        
        public CommandPriority Priority { get; } = CommandPriority.Default;
        
        public string CommandId { get; }
        
        public string CommandParams { get; }
    }
}
