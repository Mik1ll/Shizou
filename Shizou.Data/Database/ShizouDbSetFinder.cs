using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Shizou.Data.Database;

public class ShizouDbSetFinder : IDbSetFinder
{
    private readonly ConcurrentDictionary<Type, IReadOnlyList<DbSetProperty>> _cache = new();

    public virtual IReadOnlyList<DbSetProperty> FindSets(Type contextType)
        => _cache.GetOrAdd(contextType, FindSetsNonCached);

    private static DbSetProperty[] FindSetsNonCached(Type contextType)
    {
        return contextType.GetRuntimeProperties()
            .Where(p => !p.GetAccessors(true).Any(x => x.IsStatic)
                        && !p.GetIndexParameters().Any()
                        && p.DeclaringType != typeof(DbContext)
                        && p.PropertyType.GetTypeInfo().IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(ShizouDbSet<>))
            .OrderBy(p => p.Name)
            .Select(p => new DbSetProperty(
                p.Name,
                p.PropertyType.GenericTypeArguments.Single(),
                // Will not be able to assign to properties with a setter?
                null))
            .ToArray();
    }
}
