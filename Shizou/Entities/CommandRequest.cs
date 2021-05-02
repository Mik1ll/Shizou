using Microsoft.EntityFrameworkCore;
using Shizou.Enums;

namespace Shizou.Entities
{
    [Index(nameof(CommandId), IsUnique = true)]
    public class CommandRequest : Entity
    {
        public CommandType Type { get; set; }
        public CommandPriority Priority { get; set; }
        public QueueType QueueType { get; set; }
        public string CommandId { get; set; } = string.Empty;
        public string CommandParams { get; set; } = string.Empty;
    }
}
