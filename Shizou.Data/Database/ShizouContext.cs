﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shizou.Data.Models;

namespace Shizou.Data.Database;

public sealed class ShizouContext : IdentityDbContext
{
    public ShizouContext()
    {
    }

    public ShizouContext(DbContextOptions<ShizouContext> options) : base(options)
    {
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
    public DbSet<AniDbGenericFile> AniDbGenericFiles { get; set; } = null!;
    public DbSet<ScheduledCommand> ScheduledCommands { get; set; } = null!;
    public DbSet<IgnoredMessage> IgnoredMessages { get; set; } = null!;
    public DbSet<MalAniDbXref> MalAniDbXrefs { get; set; } = null!;
    public DbSet<MalAnime> MalAnimes { get; set; } = null!;
    public DbSet<FileWatchedState> FileWatchedStates { get; set; } = null!;
    public DbSet<EpisodeWatchedState> EpisodeWatchedStates { get; set; } = null!;

    public IQueryable<AniDbEpisode> EpisodesFromFile(int fileId)
    {
        return from e in AniDbEpisodes
            join r in AniDbEpisodeFileXrefs on e.Id equals r.AniDbEpisodeId
            where r.AniDbFileId == fileId
            select e;
    }

    public IQueryable<AniDbFile> FilesFromEpisode(int episodeId)
    {
        return from f in AniDbFiles
            join r in AniDbEpisodeFileXrefs on f.Id equals r.AniDbFileId
            where r.AniDbEpisodeId == episodeId
            select f;
    }

    public IQueryable<AniDbFile> FilesFromAnime(int animeId)
    {
        return from f in AniDbFiles
            join xref in AniDbEpisodeFileXrefs
                on f.Id equals xref.AniDbFileId
            join ep in AniDbEpisodes
                on xref.AniDbEpisodeId equals ep.Id
            where ep.AniDbAnimeId == animeId
            select f;
    }

    public IQueryable<AniDbFile> FilesWithLocal => from f in AniDbFiles
        where LocalFiles.Any(lf => lf.Ed2k == f.Ed2k)
        select f;

    public IQueryable<AniDbGenericFile> GenericFilesWithManualLinks => from f in AniDbGenericFiles
        where EpisodesWithManualLinks.Any(e => e.Id == f.AniDbEpisodeId)
        select f;

    public IQueryable<AniDbEpisode> EpisodesWithManualLinks => from e in AniDbEpisodes
        where e.ManualLinkLocalFiles.Any()
        select e;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder
                .UseSqlite(@$"Data Source={Path.Combine(FilePaths.ApplicationDataDir, "ShizouDB.sqlite3")};Foreign Keys=True;")
                .EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Audio)
            .WithOwner(a => a.AniDbFile);
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Subtitles)
            .WithOwner(s => s.AniDbFile);

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