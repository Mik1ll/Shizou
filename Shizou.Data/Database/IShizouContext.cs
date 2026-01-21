using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shizou.Data.Models;
using Timer = Shizou.Data.Models.Timer;

namespace Shizou.Data.Database;

/// <inheritdoc cref="DbContext" />
public interface IShizouContext : IDisposable
{
    ShizouDbSet<CommandRequest> CommandRequests { get; }

    ShizouDbSet<ImportFolder> ImportFolders { get; }

    ShizouDbSet<AniDbAnime> AniDbAnimes { get; }

    ShizouDbSet<AniDbEpisode> AniDbEpisodes { get; }

    ShizouDbSet<AniDbFile> AniDbFiles { get; }

    ShizouDbSet<AniDbNormalFile> AniDbNormalFiles { get; }

    ShizouDbSet<AniDbGenericFile> AniDbGenericFiles { get; }

    ShizouDbSet<AniDbGroup> AniDbGroups { get; }

    ShizouDbSet<LocalFile> LocalFiles { get; }

    ShizouDbSet<AniDbEpisodeFileXref> AniDbEpisodeFileXrefs { get; }

    ShizouDbSet<ScheduledCommand> ScheduledCommands { get; }

    ShizouDbSet<MalAniDbXref> MalAniDbXrefs { get; }

    ShizouDbSet<MalAnime> MalAnimes { get; }

    ShizouDbSet<FileWatchedState> FileWatchedStates { get; }

    ShizouDbSet<HangingEpisodeFileXref> HangingEpisodeFileXrefs { get; }

    ShizouDbSet<Timer> Timers { get; }

    ShizouDbSet<AniDbAnimeRelation> AniDbAnimeRelations { get; }

    ShizouDbSet<AnimeFilter> AnimeFilters { get; }

    ShizouDbSet<AniDbCreator> AniDbCreators { get; }

    ShizouDbSet<AniDbCharacter> AniDbCharacters { get; }

    ShizouDbSet<AniDbCredit> AniDbCredits { get; }

    #region BaseMembers

    /// <inheritdoc cref="DbContext.Database" />
    DatabaseFacade Database { get; }

    /// <inheritdoc cref="DbContext.ChangeTracker" />
    ChangeTracker ChangeTracker { get; }

    /// <inheritdoc cref="DbContext.Model" />
    IModel Model { get; }

    /// <inheritdoc cref="DbContext.ContextId" />
    DbContextId ContextId { get; }


    /// <inheritdoc cref="DbContext.Set{TEntity}()" />
    ShizouDbSet<TEntity> Set<TEntity>() where TEntity : class;

    /// <inheritdoc cref="DbContext.Set{TEntity}(string)" />
    ShizouDbSet<TEntity> Set<TEntity>(string name) where TEntity : class;

    /// <inheritdoc cref="DbContext.SaveChanges()" />
    int SaveChanges();

    /// <inheritdoc cref="DbContext.SaveChanges(bool)" />
    int SaveChanges(bool acceptAllChangesOnSuccess);

    /// <inheritdoc cref="DbContext.Entry{TEntity}(TEntity)" />
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    /// <inheritdoc cref="DbContext.Entry(object)" />
    EntityEntry Entry(object entity);

    /// <inheritdoc cref="DbContext.Add{TEntity}(TEntity)" />
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

    /// <inheritdoc cref="DbContext.Add(object)" />
    EntityEntry Add(object entity);

    /// <inheritdoc cref="DbContext.Attach{TEntity}(TEntity)" />
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

    /// <inheritdoc cref="DbContext.Attach(object)" />
    EntityEntry Attach(object entity);

    /// <inheritdoc cref="DbContext.Update{TEntity}(TEntity)" />
    EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;

    /// <inheritdoc cref="DbContext.Update(object)" />
    EntityEntry Update(object entity);

    /// <inheritdoc cref="DbContext.Remove{TEntity}(TEntity)" />
    EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;

    /// <inheritdoc cref="DbContext.Remove(object)" />
    EntityEntry Remove(object entity);

    /// <inheritdoc cref="DbContext.AddRange(object[])" />
    void AddRange(params object[] entities);

    /// <inheritdoc cref="DbContext.AddRange(IEnumerable{object})" />
    void AddRange(IEnumerable<object> entities);

    /// <inheritdoc cref="DbContext.AttachRange(object[])" />
    void AttachRange(params object[] entities);

    /// <inheritdoc cref="DbContext.AttachRange(IEnumerable{object})" />
    void AttachRange(IEnumerable<object> entities);

    /// <inheritdoc cref="DbContext.UpdateRange(object[])" />
    void UpdateRange(params object[] entities);

    /// <inheritdoc cref="DbContext.UpdateRange(IEnumerable{object})" />
    void UpdateRange(IEnumerable<object> entities);

    /// <inheritdoc cref="DbContext.RemoveRange(object[])" />
    void RemoveRange(params object[] entities);

    /// <inheritdoc cref="DbContext.RemoveRange(IEnumerable{object})" />
    void RemoveRange(IEnumerable<object> entities);

    /// <inheritdoc cref="DbContext.Find(Type, object[])" />
    object? Find(Type entityType, params object?[]? keyValues);

    /// <inheritdoc cref="DbContext.Find{TEntity}(object[])" />
    TEntity? Find<TEntity>(params object?[]? keyValues) where TEntity : class;

    /// <inheritdoc cref="DbContext.FromExpression{TResult}(Expression{Func{IQueryable{TResult}}})" />
    IQueryable<TResult> FromExpression<TResult>(Expression<Func<IQueryable<TResult>>> expression);

    /// <inheritdoc cref="DbContext.SavingChanges" />
    event EventHandler<SavingChangesEventArgs>? SavingChanges;

    /// <inheritdoc cref="DbContext.SavedChanges" />
    event EventHandler<SavedChangesEventArgs>? SavedChanges;

    /// <inheritdoc cref="DbContext.SaveChangesFailed" />
    event EventHandler<SaveChangesFailedEventArgs>? SaveChangesFailed;

    #endregion
}
