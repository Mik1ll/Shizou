using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Shizou.Data.Database;

public class ShizouDbSet<TEntity> : IShizouDbSet<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;

    public ShizouDbSet(DbSet<TEntity> dbSet) => _dbSet = dbSet;

    public bool ContainsListCollection => ((IListSource)_dbSet).ContainsListCollection;

    public Type ElementType => ((IQueryable)_dbSet).ElementType;

    public Expression Expression => ((IQueryable)_dbSet).Expression;

    public IQueryProvider Provider => ((IQueryable)_dbSet).Provider;

    public IEntityType EntityType => _dbSet.EntityType;
    public LocalView<TEntity> Local => _dbSet.Local;
    public IEnumerator<TEntity> GetEnumerator() => ((IEnumerable<TEntity>)_dbSet).GetEnumerator();

    public IList GetList() => ((IListSource)_dbSet).GetList();

    public IQueryable<TEntity> AsQueryable() => _dbSet.AsQueryable();

    public TEntity? Find(params object?[]? keyValues) => _dbSet.Find(keyValues);

    public EntityEntry<TEntity> Add(TEntity entity) => _dbSet.Add(entity);

    public EntityEntry<TEntity> Attach(TEntity entity) => _dbSet.Attach(entity);

    public EntityEntry<TEntity> Remove(TEntity entity) => _dbSet.Remove(entity);

    public EntityEntry<TEntity> Update(TEntity entity) => _dbSet.Update(entity);

    public void AddRange(params TEntity[] entities) => _dbSet.AddRange(entities);

    public void AddRange(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);

    public void AttachRange(params TEntity[] entities) => _dbSet.AttachRange(entities);

    public void AttachRange(IEnumerable<TEntity> entities) => _dbSet.AttachRange(entities);

    public void RemoveRange(params TEntity[] entities) => _dbSet.RemoveRange(entities);

    public void RemoveRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

    public void UpdateRange(params TEntity[] entities) => _dbSet.UpdateRange(entities);

    public void UpdateRange(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);

    public EntityEntry<TEntity> Entry(TEntity entity) => _dbSet.Entry(entity);

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dbSet).GetEnumerator();
}
