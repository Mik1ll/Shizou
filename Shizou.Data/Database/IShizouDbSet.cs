using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Shizou.Data.Database;

public interface IShizouDbSet<TEntity> where TEntity : class
{
    bool Equals(object? obj);
    int GetHashCode();
    string? ToString();
    IQueryable<TEntity> AsQueryable();
    TEntity? Find(params object?[]? keyValues);
    EntityEntry<TEntity> Add(TEntity entity);
    EntityEntry<TEntity> Attach(TEntity entity);
    EntityEntry<TEntity> Remove(TEntity entity);
    EntityEntry<TEntity> Update(TEntity entity);
    void AddRange(params TEntity[] entities);
    void AddRange(IEnumerable<TEntity> entities);
    void AttachRange(params TEntity[] entities);
    void AttachRange(IEnumerable<TEntity> entities);
    void RemoveRange(params TEntity[] entities);
    void RemoveRange(IEnumerable<TEntity> entities);
    void UpdateRange(params TEntity[] entities);
    void UpdateRange(IEnumerable<TEntity> entities);
    EntityEntry<TEntity> Entry(TEntity entity);
    IEntityType EntityType { get; }
    LocalView<TEntity> Local { get; }
}
