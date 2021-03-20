using Shizou.Commands;

namespace Shizou.Entities
{
    public class CommandRequest : Entity
    {
        public CommandType Type { get; set; }
        
        public CommandPriority Priority { get; set; } = CommandPriority.Default;

        public string CommandId { get; set; } = null!;

        public string CommandParams { get; set; } = null!;
    }
}
