using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Models;

namespace Shizou.Database;

public sealed class ShizouContext : DbContext
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
    public DbSet<AniDbMyListEntry> AniDbMyListEntries { get; set; } = null!;

    public IQueryable<AniDbEpisode> GetEpisodesFromFile(int fileId)
    {
        return from e in AniDbEpisodes
            join r in AniDbEpisodeFileXrefs on e.Id equals r.AniDbEpisodeId
            where r.AniDbFileId == fileId
            select e;
    }

    public IQueryable<AniDbFile> GetFilesFromEpisode(int episodeId)
    {
        return from f in AniDbFiles
            join r in AniDbEpisodeFileXrefs on f.Id equals r.AniDbFileId
            where r.AniDbEpisodeId == episodeId
            select f;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite(@$"Data Source={Path.Combine(Constants.ApplicationData, "ShizouDB.sqlite3")};Foreign Keys=True;")
            .EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AniDbEpisode>()
            .HasMany(e => e.ManualLinkLocalFiles)
            .WithOne(e => e.ManualLinkEpisode!)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Audio)
            .WithOwner(a => a.AniDbFile);
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Subtitles)
            .WithOwner(s => s.AniDbFile);
        modelBuilder.Entity<AniDbEpisodeFileXref>()
            .HasKey(r => new { r.AniDbEpisodeId, r.AniDbFileId });
    }

    public void ReplaceList<T, TKey>(List<T> source, List<T> destination, Func<T, TKey> keySelector)
        where TKey : IEquatable<TKey>
        where T : notnull
    {
        foreach (var item in source)
            if (destination.FirstOrDefault(a => keySelector(a).Equals(keySelector(item))) is var eItem && eItem is null)
            {
                Add(item);
                destination.Add(item);
            }
            else
                Entry(eItem).CurrentValues.SetValues(item);
        var removeItems = destination.Where(x => !source.Any(a => keySelector(a).Equals(keySelector(x)))).ToList();
        RemoveRange(removeItems.Cast<object>().ToArray());
        destination.RemoveAll(a => removeItems.Contains(a));
    }
}
