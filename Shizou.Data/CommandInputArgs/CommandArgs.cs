using System.Text.Json.Serialization;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Data.CommandInputArgs;

public abstract record CommandArgs(
    [property: JsonIgnore] string CommandId,
    [property: JsonIgnore] CommandPriority CommandPriority,
    [property: JsonIgnore] QueueType QueueType)
{
    [JsonIgnore]
    public CommandRequest CommandRequest => new()
    {
        Priority = CommandPriority,
        QueueType = QueueType,
        CommandId = CommandId,
        CommandArgs = this
    };
}
