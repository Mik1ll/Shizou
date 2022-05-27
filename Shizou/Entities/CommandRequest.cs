using Microsoft.EntityFrameworkCore;
using Shizou.CommandProcessors;
using Shizou.Commands;
using Shizou.Dtos;

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


        public override CommandRequestDto ToDto()
        {
            return new CommandRequestDto
            {
                Id = Id,
                Priority = Priority,
                Type = Type,
                CommandId = CommandId,
                CommandParams = CommandParams,
                QueueType = QueueType
            };
        }
    }
}
