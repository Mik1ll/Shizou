using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;
using Shizou.Data.Utilities;
using Timer = Shizou.Data.Models.Timer;

namespace Shizou.Data.Database;

public sealed class ShizouContext : IdentityDbContext, IShizouContext
{
    public ShizouContext()
    {
    }

    public ShizouContext(DbContextOptions<ShizouContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = FilePaths.DatabasePath,
                ForeignKeys = true,
                Cache = SqliteCacheMode.Private,
                Pooling = true
            }.ConnectionString;
            optionsBuilder
                .UseSqlite(connectionString)
                .EnableSensitiveDataLogging();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Audio, builder => builder.ToJson());
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Subtitles, builder => builder.ToJson());
        modelBuilder.Entity<AniDbFile>()
            .OwnsOne(f => f.Video, builder => builder.ToJson());
        modelBuilder.Entity<AniDbAnime>()
            .HasMany(a => a.MalAnimes)
            .WithMany(a => a.AniDbAnimes)
            .UsingEntity<MalAniDbXref>();
        modelBuilder.Entity<AniDbEpisode>()
            .HasMany(ep => ep.AniDbFiles)
            .WithMany(f => f.AniDbEpisodes)
            .UsingEntity<AniDbEpisodeFileXref>();
        modelBuilder.Entity<CommandRequest>()
            .Property(cr => cr.CommandArgs)
            .HasConversion<CommandArgsConverter>();
        modelBuilder.Entity<ScheduledCommand>()
            .Property(cr => cr.CommandArgs)
            .HasConversion<CommandArgsConverter>();
        modelBuilder.Entity<AnimeFilter>()
            .Property(f => f.Criteria)
            .HasConversion<AnimeCriterionConverter>();

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<DateTimeConverter>();
        configurationBuilder
            .Properties<DateTime?>()
            .HaveConversion<NullableDateTimeConverter>();

        base.ConfigureConventions(configurationBuilder);
    }

    private class CommandArgsConverter : ValueConverter<CommandArgs, string>
    {
        public CommandArgsConverter() : base(
            ca => JsonSerializer.Serialize(ca, PolymorphicJsonTypeInfo<CommandArgs>.CreateJsonTypeInfo(null)),
            ca => JsonSerializer.Deserialize(ca, PolymorphicJsonTypeInfo<CommandArgs>.CreateJsonTypeInfo(null))!)
        {
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class DateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public DateTimeConverter() : base(v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class NullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableDateTimeConverter() : base(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }

    private class AnimeCriterionConverter : ValueConverter<OrAnyCriterion, string>
    {
        public AnimeCriterionConverter() : base(
            v => JsonSerializer.Serialize(v, new JsonSerializerOptions
            {
                TypeInfoResolver = new PolymorphicJsonTypeResolver<TermCriterion>()
            }),
            v => JsonSerializer.Deserialize<OrAnyCriterion>(v, new JsonSerializerOptions
            {
                TypeInfoResolver = new PolymorphicJsonTypeResolver<TermCriterion>()
            })!)
        {
        }
    }

    #region DbSets

    IShizouDbSet<TEntity> IShizouContext.Set<TEntity>() => new ShizouDbSet<TEntity>(base.Set<TEntity>());
    IShizouDbSet<TEntity> IShizouContext.Set<TEntity>(string name) => new ShizouDbSet<TEntity>(base.Set<TEntity>(name));
    public DbSet<CommandRequest> CommandRequests { get; set; } = null!;
    IShizouDbSet<CommandRequest> IShizouContext.CommandRequests => new ShizouDbSet<CommandRequest>(CommandRequests);
    public DbSet<ImportFolder> ImportFolders { get; set; } = null!;
    IShizouDbSet<ImportFolder> IShizouContext.ImportFolders => new ShizouDbSet<ImportFolder>(ImportFolders);
    public DbSet<AniDbAnime> AniDbAnimes { get; set; } = null!;
    IShizouDbSet<AniDbAnime> IShizouContext.AniDbAnimes => new ShizouDbSet<AniDbAnime>(AniDbAnimes);
    public DbSet<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
    IShizouDbSet<AniDbEpisode> IShizouContext.AniDbEpisodes => new ShizouDbSet<AniDbEpisode>(AniDbEpisodes);
    public DbSet<AniDbFile> AniDbFiles { get; set; } = null!;
    IShizouDbSet<AniDbFile> IShizouContext.AniDbFiles => new ShizouDbSet<AniDbFile>(AniDbFiles);
    public DbSet<AniDbGroup> AniDbGroups { get; set; } = null!;
    IShizouDbSet<AniDbGroup> IShizouContext.AniDbGroups => new ShizouDbSet<AniDbGroup>(AniDbGroups);
    public DbSet<LocalFile> LocalFiles { get; set; } = null!;
    IShizouDbSet<LocalFile> IShizouContext.LocalFiles => new ShizouDbSet<LocalFile>(LocalFiles);
    public DbSet<AniDbEpisodeFileXref> AniDbEpisodeFileXrefs { get; set; } = null!;
    IShizouDbSet<AniDbEpisodeFileXref> IShizouContext.AniDbEpisodeFileXrefs => new ShizouDbSet<AniDbEpisodeFileXref>(AniDbEpisodeFileXrefs);
    public DbSet<ScheduledCommand> ScheduledCommands { get; set; } = null!;
    IShizouDbSet<ScheduledCommand> IShizouContext.ScheduledCommands => new ShizouDbSet<ScheduledCommand>(ScheduledCommands);
    public DbSet<MalAniDbXref> MalAniDbXrefs { get; set; } = null!;
    IShizouDbSet<MalAniDbXref> IShizouContext.MalAniDbXrefs => new ShizouDbSet<MalAniDbXref>(MalAniDbXrefs);
    public DbSet<MalAnime> MalAnimes { get; set; } = null!;
    IShizouDbSet<MalAnime> IShizouContext.MalAnimes => new ShizouDbSet<MalAnime>(MalAnimes);
    public DbSet<FileWatchedState> FileWatchedStates { get; set; } = null!;
    IShizouDbSet<FileWatchedState> IShizouContext.FileWatchedStates => new ShizouDbSet<FileWatchedState>(FileWatchedStates);
    public DbSet<EpisodeWatchedState> EpisodeWatchedStates { get; set; } = null!;
    IShizouDbSet<EpisodeWatchedState> IShizouContext.EpisodeWatchedStates => new ShizouDbSet<EpisodeWatchedState>(EpisodeWatchedStates);
    public DbSet<HangingEpisodeFileXref> HangingEpisodeFileXrefs { get; set; } = null!;
    IShizouDbSet<HangingEpisodeFileXref> IShizouContext.HangingEpisodeFileXrefs => new ShizouDbSet<HangingEpisodeFileXref>(HangingEpisodeFileXrefs);
    public DbSet<Timer> Timers { get; set; } = null!;
    IShizouDbSet<Timer> IShizouContext.Timers => new ShizouDbSet<Timer>(Timers);
    public DbSet<AniDbAnimeRelation> AniDbAnimeRelations { get; set; } = null!;
    IShizouDbSet<AniDbAnimeRelation> IShizouContext.AniDbAnimeRelations => new ShizouDbSet<AniDbAnimeRelation>(AniDbAnimeRelations);
    public DbSet<AnimeFilter> AnimeFilters { get; set; } = null!;
    IShizouDbSet<AnimeFilter> IShizouContext.AnimeFilters => new ShizouDbSet<AnimeFilter>(AnimeFilters);

    #endregion
}
