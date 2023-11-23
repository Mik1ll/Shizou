using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shizou.Data.Utilities;

public class PolymorphicJsonTypeResolver<T> : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = base.GetTypeInfo(type, options);
        if (typeInfo.Type == typeof(T))
            return PolymorphicJsonTypeInfo<T>.CreateJsonTypeInfo(options);
        return typeInfo;
    }
}
