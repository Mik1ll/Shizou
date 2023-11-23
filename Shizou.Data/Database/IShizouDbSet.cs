using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Shizou.Data.Database;

/// <inheritdoc cref="DbSet{TEntity}" />
public interface IShizouDbSet<TEntity> : IInfrastructure<IServiceProvider>, IListSource, IQueryable<TEntity> where TEntity : class
{
    /// <inheritdoc cref="DbSet{TEntity}.EntityType" />
    IEntityType EntityType { get; }

    /// <inheritdoc cref="DbSet{TEntity}.Local" />
    LocalView<TEntity> Local { get; }

    /// <inheritdoc cref="DbSet{TEntity}.AsQueryable()" />
    IQueryable<TEntity> AsQueryable();

    /// <inheritdoc cref="DbSet{TEntity}.Find(object[])" />
    TEntity? Find(params object?[]? keyValues);

    /// <inheritdoc cref="DbSet{TEntity}.Add(TEntity)" />
    EntityEntry<TEntity> Add(TEntity entity);

    /// <inheritdoc cref="DbSet{TEntity}.Attach(TEntity)" />
    EntityEntry<TEntity> Attach(TEntity entity);

    /// <inheritdoc cref="DbSet{TEntity}.Remove(TEntity)" />
    EntityEntry<TEntity> Remove(TEntity entity);

    /// <inheritdoc cref="DbSet{TEntity}.Update(TEntity)" />
    EntityEntry<TEntity> Update(TEntity entity);

    /// <inheritdoc cref="DbSet{TEntity}.AddRange(TEntity[])" />
    void AddRange(params TEntity[] entities);

    /// <inheritdoc cref="DbSet{TEntity}.AddRange(IEnumerable{TEntity})" />
    void AddRange(IEnumerable<TEntity> entities);

    /// <inheritdoc cref="DbSet{TEntity}.AttachRange(TEntity[])" />
    void AttachRange(params TEntity[] entities);

    /// <inheritdoc cref="DbSet{TEntity}.AttachRange(IEnumerable{TEntity})" />
    void AttachRange(IEnumerable<TEntity> entities);

    /// <inheritdoc cref="DbSet{TEntity}.RemoveRange(TEntity[])" />
    void RemoveRange(params TEntity[] entities);

    /// <inheritdoc cref="DbSet{TEntity}.RemoveRange(IEnumerable{TEntity})" />
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <inheritdoc cref="DbSet{TEntity}.UpdateRange(TEntity[])" />
    void UpdateRange(params TEntity[] entities);

    /// <inheritdoc cref="DbSet{TEntity}.UpdateRange(IEnumerable{TEntity})" />
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <inheritdoc cref="DbSet{TEntity}.Entry(TEntity)" />
    EntityEntry<TEntity> Entry(TEntity entity);
}
