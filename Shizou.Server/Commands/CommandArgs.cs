using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Data.Utilities;

namespace Shizou.Server.Commands;

public abstract record CommandArgs(
    [property: JsonIgnore] string CommandId,
    [property: JsonIgnore] Type CommandType,
    [property: JsonIgnore] CommandPriority CommandPriority,
    [property: JsonIgnore] QueueType QueueType)
{
    [JsonIgnore]
    public CommandRequest CommandRequest => new()
    {
        Priority = CommandPriority,
        QueueType = QueueType,
        CommandId = CommandId,
        CommandArgs = JsonSerializer.Serialize(this, PolymorphicJsonTypeInfo<CommandArgs>.CreateJsonTypeInfo())
    };
}
