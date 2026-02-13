using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shizou.Data.Utilities;

public class PolymorphicJsonTypeResolver<T> : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) =>
        type == typeof(T) ? PolymorphicJsonTypeInfo<T>.CreateJsonTypeInfo(options) : base.GetTypeInfo(type, options);
}
