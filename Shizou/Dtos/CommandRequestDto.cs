using Shizou.CommandProcessors;
using Shizou.Commands;

namespace Shizou.Dtos
{
    public class CommandRequestDto : EntityDto
    {
        public CommandType Type { get; set; }
        public CommandPriority Priority { get; set; }
        public QueueType QueueType { get; set; }
        public string CommandId { get; set; } = string.Empty;
        public string CommandParams { get; set; } = string.Empty;
    }
}
