using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shizou.Data.Models;
using Timer = Shizou.Data.Models.Timer;

namespace Shizou.Data.Database;

public interface IShizouContext
{
    IShizouDbSet<CommandRequest> CommandRequests { get; }
    IShizouDbSet<ImportFolder> ImportFolders { get; }
    IShizouDbSet<AniDbAnime> AniDbAnimes { get; }
    IShizouDbSet<AniDbEpisode> AniDbEpisodes { get; }
    IShizouDbSet<AniDbFile> AniDbFiles { get; }
    IShizouDbSet<AniDbGroup> AniDbGroups { get; }
    IShizouDbSet<AniDbAudio> AniDbAudio { get; }
    IShizouDbSet<AniDbSubtitle> AniDbSubtitles { get; }
    IShizouDbSet<LocalFile> LocalFiles { get; }
    IShizouDbSet<AniDbEpisodeFileXref> AniDbEpisodeFileXrefs { get; }
    IShizouDbSet<ScheduledCommand> ScheduledCommands { get; }
    IShizouDbSet<MalAniDbXref> MalAniDbXrefs { get; }
    IShizouDbSet<MalAnime> MalAnimes { get; }
    IShizouDbSet<FileWatchedState> FileWatchedStates { get; }
    IShizouDbSet<EpisodeWatchedState> EpisodeWatchedStates { get; }
    IShizouDbSet<HangingEpisodeFileXref> HangingEpisodeFileXrefs { get; }
    IShizouDbSet<Timer> Timers { get; }
    IShizouDbSet<AniDbAnimeRelation> AniDbAnimeRelations { get; }
    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    IModel Model { get; }
    DbContextId ContextId { get; }
    bool Equals(object? obj);
    int GetHashCode();
    string? ToString();
    IShizouDbSet<TEntity> Set<TEntity>() where TEntity : class;
    IShizouDbSet<TEntity> Set<TEntity>(string name) where TEntity : class;
    int SaveChanges();
    int SaveChanges(bool acceptAllChangesOnSuccess);
    void Dispose();
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry Entry(object entity);
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry Add(object entity);
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry Attach(object entity);
    EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry Update(object entity);
    EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;
    EntityEntry Remove(object entity);
    void AddRange(params object[] entities);
    void AddRange(IEnumerable<object> entities);
    void AttachRange(params object[] entities);
    void AttachRange(IEnumerable<object> entities);
    void UpdateRange(params object[] entities);
    void UpdateRange(IEnumerable<object> entities);
    void RemoveRange(params object[] entities);
    void RemoveRange(IEnumerable<object> entities);
    object? Find(Type entityType, params object?[]? keyValues);
    TEntity? Find<TEntity>(params object?[]? keyValues) where TEntity : class;
    IQueryable<TResult> FromExpression<TResult>(Expression<Func<IQueryable<TResult>>> expression);
    event EventHandler<SavingChangesEventArgs>? SavingChanges;
    event EventHandler<SavedChangesEventArgs>? SavedChanges;
    event EventHandler<SaveChangesFailedEventArgs>? SaveChangesFailed;
}
