using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shizou.Data.Utilities;

public static class PolymorphicJsonTypeInfo<T>
{
    private static readonly Type[] ArgTypes = (from type in Assembly.GetExecutingAssembly().GetTypes()
        where typeof(T).IsAssignableFrom(type) && type != typeof(T)
        select type).ToArray();

    public static JsonTypeInfo<T> CreateJsonTypeInfo(JsonSerializerOptions? options = null)
    {
        options ??= JsonSerializerOptions.Default;
        var typeInfo = JsonTypeInfo.CreateJsonTypeInfo<T>(options);
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
