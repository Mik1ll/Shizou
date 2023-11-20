﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shizou.Data.Models;
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

    IShizouDbSet<CommandRequest> IShizouContext.CommandRequests => new ShizouDbSet<CommandRequest>(CommandRequests);
    IShizouDbSet<ImportFolder> IShizouContext.ImportFolders => new ShizouDbSet<ImportFolder>(ImportFolders);
    IShizouDbSet<AniDbAnime> IShizouContext.AniDbAnimes => new ShizouDbSet<AniDbAnime>(AniDbAnimes);
    IShizouDbSet<AniDbEpisode> IShizouContext.AniDbEpisodes => new ShizouDbSet<AniDbEpisode>(AniDbEpisodes);
    IShizouDbSet<AniDbFile> IShizouContext.AniDbFiles => new ShizouDbSet<AniDbFile>(AniDbFiles);
    IShizouDbSet<AniDbGroup> IShizouContext.AniDbGroups => new ShizouDbSet<AniDbGroup>(AniDbGroups);
    IShizouDbSet<AniDbAudio> IShizouContext.AniDbAudio => new ShizouDbSet<AniDbAudio>(AniDbAudio);
    IShizouDbSet<AniDbSubtitle> IShizouContext.AniDbSubtitles => new ShizouDbSet<AniDbSubtitle>(AniDbSubtitles);
    IShizouDbSet<LocalFile> IShizouContext.LocalFiles => new ShizouDbSet<LocalFile>(LocalFiles);
    IShizouDbSet<AniDbEpisodeFileXref> IShizouContext.AniDbEpisodeFileXrefs => new ShizouDbSet<AniDbEpisodeFileXref>(AniDbEpisodeFileXrefs);
    IShizouDbSet<ScheduledCommand> IShizouContext.ScheduledCommands => new ShizouDbSet<ScheduledCommand>(ScheduledCommands);
    IShizouDbSet<MalAniDbXref> IShizouContext.MalAniDbXrefs => new ShizouDbSet<MalAniDbXref>(MalAniDbXrefs);
    IShizouDbSet<MalAnime> IShizouContext.MalAnimes => new ShizouDbSet<MalAnime>(MalAnimes);
    IShizouDbSet<FileWatchedState> IShizouContext.FileWatchedStates => new ShizouDbSet<FileWatchedState>(FileWatchedStates);
    IShizouDbSet<EpisodeWatchedState> IShizouContext.EpisodeWatchedStates => new ShizouDbSet<EpisodeWatchedState>(EpisodeWatchedStates);
    IShizouDbSet<HangingEpisodeFileXref> IShizouContext.HangingEpisodeFileXrefs => new ShizouDbSet<HangingEpisodeFileXref>(HangingEpisodeFileXrefs);
    IShizouDbSet<Timer> IShizouContext.Timers => new ShizouDbSet<Timer>(Timers);
    IShizouDbSet<AniDbAnimeRelation> IShizouContext.AniDbAnimeRelations => new ShizouDbSet<AniDbAnimeRelation>(AniDbAnimeRelations);

    IShizouDbSet<TEntity> IShizouContext.Set<TEntity>()
    {
        return new ShizouDbSet<TEntity>(base.Set<TEntity>());
    }

    IShizouDbSet<TEntity> IShizouContext.Set<TEntity>(string name)
    {
        return new ShizouDbSet<TEntity>(base.Set<TEntity>(name));
    }

    public DbSet<CommandRequest> CommandRequests { get; set; } = null!;
    public DbSet<ImportFolder> ImportFolders { get; set; } = null!;
    public DbSet<AniDbAnime> AniDbAnimes { get; set; } = null!;
    public DbSet<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
    public DbSet<AniDbFile> AniDbFiles { get; set; } = null!;
    public DbSet<AniDbGroup> AniDbGroups { get; set; } = null!;
    public DbSet<AniDbAudio> AniDbAudio { get; set; } = null!;
    public DbSet<AniDbSubtitle> AniDbSubtitles { get; set; } = null!;
    public DbSet<LocalFile> LocalFiles { get; set; } = null!;
    public DbSet<AniDbEpisodeFileXref> AniDbEpisodeFileXrefs { get; set; } = null!;
    public DbSet<ScheduledCommand> ScheduledCommands { get; set; } = null!;
    public DbSet<MalAniDbXref> MalAniDbXrefs { get; set; } = null!;
    public DbSet<MalAnime> MalAnimes { get; set; } = null!;
    public DbSet<FileWatchedState> FileWatchedStates { get; set; } = null!;
    public DbSet<EpisodeWatchedState> EpisodeWatchedStates { get; set; } = null!;
    public DbSet<HangingEpisodeFileXref> HangingEpisodeFileXrefs { get; set; } = null!;
    public DbSet<Timer> Timers { get; set; } = null!;
    public DbSet<AniDbAnimeRelation> AniDbAnimeRelations { get; set; } = null!;

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
            .OwnsMany(f => f.Audio)
            .WithOwner(a => a.AniDbFile);
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Subtitles)
            .WithOwner(s => s.AniDbFile);
        modelBuilder.Entity<AniDbAnime>()
            .HasMany(a => a.MalAnimes)
            .WithMany(a => a.AniDbAnimes)
            .UsingEntity<MalAniDbXref>();
        modelBuilder.Entity<AniDbEpisode>()
            .HasMany(ep => ep.AniDbFiles)
            .WithMany(f => f.AniDbEpisodes)
            .UsingEntity<AniDbEpisodeFileXref>();

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
}
