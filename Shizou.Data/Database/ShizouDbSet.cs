using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Shizou.Data.Database;

public class ShizouDbSet<TEntity> : IShizouDbSet<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;

    public ShizouDbSet(DbSet<TEntity> dbSet)
    {
        _dbSet = dbSet;
    }

    public IQueryable<TEntity> AsQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public TEntity? Find(params object?[]? keyValues)
    {
        return _dbSet.Find(keyValues);
    }

    public EntityEntry<TEntity> Add(TEntity entity)
    {
        return _dbSet.Add(entity);
    }

    public EntityEntry<TEntity> Attach(TEntity entity)
    {
        return _dbSet.Attach(entity);
    }

    public EntityEntry<TEntity> Remove(TEntity entity)
    {
        return _dbSet.Remove(entity);
    }

    public EntityEntry<TEntity> Update(TEntity entity)
    {
        return _dbSet.Update(entity);
    }

    public void AddRange(params TEntity[] entities)
    {
        _dbSet.AddRange(entities);
    }

    public void AddRange(IEnumerable<TEntity> entities)
    {
        _dbSet.AddRange(entities);
    }

    public void AttachRange(params TEntity[] entities)
    {
        _dbSet.AttachRange(entities);
    }

    public void AttachRange(IEnumerable<TEntity> entities)
    {
        _dbSet.AttachRange(entities);
    }

    public void RemoveRange(params TEntity[] entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public void UpdateRange(params TEntity[] entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public EntityEntry<TEntity> Entry(TEntity entity)
    {
        return _dbSet.Entry(entity);
    }

    public IEntityType EntityType => _dbSet.EntityType;
    public LocalView<TEntity> Local => _dbSet.Local;
}
