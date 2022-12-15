using Microsoft.EntityFrameworkCore;
using Shizou.CommandProcessors;
using Shizou.Commands;

namespace Shizou.Models;

[Index(nameof(CommandId), IsUnique = true)]
public class CommandRequest : IEntity
{
    public int Id { get; set; }
    public CommandType Type { get; set; }
    public CommandPriority Priority { get; set; }
    public QueueType QueueType { get; set; }
    public string CommandId { get; set; } = string.Empty;
    public string CommandParams { get; set; } = string.Empty;
}