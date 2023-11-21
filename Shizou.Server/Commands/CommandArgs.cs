using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shizou.Server.Commands;

public abstract record CommandArgs(string CommandId)
{
    private static readonly Type[] ArgTypes = (from type in Assembly.GetExecutingAssembly().GetTypes()
        where typeof(CommandArgs).IsAssignableFrom(type) && type != typeof(CommandArgs)
        select type).ToArray();

    [JsonIgnore]
    public string CommandId { get; } = CommandId;

    public static JsonTypeInfo<CommandArgs> GetJsonTypeInfo(JsonSerializerOptions? options = null)
    {
        options ??= JsonSerializerOptions.Default;
        var typeInfo = JsonTypeInfo.CreateJsonTypeInfo<CommandArgs>(options);
        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };
        foreach (var argType in ArgTypes)
            typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(argType, argType.Name));
        return typeInfo;
    }
}
