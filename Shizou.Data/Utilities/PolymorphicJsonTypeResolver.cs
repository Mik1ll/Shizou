using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shizou.Data.Utilities;

public class PolymorphicJsonTypeResolver<T> : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type == typeof(T))
            return PolymorphicJsonTypeInfo<T>.CreateJsonTypeInfo(options);
        return base.GetTypeInfo(type, options);
    }
}
