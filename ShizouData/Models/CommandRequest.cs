using Microsoft.EntityFrameworkCore;
using ShizouCommon.Enums;

namespace ShizouData.Models;

[Index(nameof(CommandId), IsUnique = true)]
public class CommandRequest : IEntity
{
    public int Id { get; set; }
    public required CommandType Type { get; set; }
    public required CommandPriority Priority { get; set; }
    public required QueueType QueueType { get; set; }
    public required string CommandId { get; set; }
    public required string CommandArgs { get; set; }
}
