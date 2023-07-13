using Shizou.Data.Enums;

namespace Shizou.Data.Models;

public interface ICommandRequest : IEntity
{
    CommandType Type { get; set; }
    CommandPriority Priority { get; set; }
    QueueType QueueType { get; set; }
    string CommandId { get; set; }
    string CommandArgs { get; set; }
}
