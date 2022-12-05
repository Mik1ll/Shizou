using System.Text.Json.Serialization;

namespace Shizou.Commands;

public abstract record CommandParams(string CommandId)
{
    [JsonIgnore]
    public string CommandId { get; } = CommandId;
}