using System.Text.Json.Serialization;

namespace Shizou.Server.Commands;

public abstract record CommandArgs(string CommandId)
{
    [JsonIgnore]
    public string CommandId { get; } = CommandId;
}