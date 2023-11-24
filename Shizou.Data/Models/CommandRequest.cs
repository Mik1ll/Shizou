using Microsoft.EntityFrameworkCore;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

[Index(nameof(CommandId), IsUnique = true)]
public class CommandRequest
{
    public int Id { get; set; }
    public required CommandPriority Priority { get; set; }
    public required QueueType QueueType { get; set; }
    public required string CommandId { get; set; }
    public required CommandArgs CommandArgs { get; set; }
}
