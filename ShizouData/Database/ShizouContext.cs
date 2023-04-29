using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShizouData.Models;

namespace ShizouData.Database;

public sealed class ShizouContext : IdentityDbContext
{
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
    public DbSet<AniDbGenericFile> AniDbGenericFiles { get; set; } = null!;

    public IQueryable<AniDbEpisode> AniDbEpisodesFromFile(int aniDbFileId)
    {
        return from e in AniDbEpisodes
            join r in AniDbEpisodeFileXrefs on e.Id equals r.AniDbEpisodeId
            where r.AniDbFileId == aniDbFileId
            select e;
    }

    public IQueryable<AniDbFile> FilesFromAniDbEpisode(int episodeId)
    {
        return from f in AniDbFiles
            join r in AniDbEpisodeFileXrefs on f.Id equals r.AniDbFileId
            where r.AniDbEpisodeId == episodeId
            select f;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite(@$"Data Source={Path.Combine(FilePaths.ApplicationDataDir, "ShizouDB.sqlite3")};Foreign Keys=True;")
            .EnableSensitiveDataLogging();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Audio)
            .WithOwner(a => a.AniDbFile);
        modelBuilder.Entity<AniDbFile>()
            .OwnsMany(f => f.Subtitles)
            .WithOwner(s => s.AniDbFile);
        modelBuilder.Entity<AniDbEpisode>()
            .HasMany(e => e.ManualLinkLocalFiles)
            .WithMany(e => e.ManualLinkEpisodes)
            .UsingEntity(j => j.ToTable("ManualLinkXrefs"));
        base.OnModelCreating(modelBuilder);
    }

    public void ReplaceList<T, TKey>(List<T> source, List<T> destination, Func<T, TKey> keySelector)
        where TKey : IEquatable<TKey>
        where T : notnull
    {
        var removeItems = destination.Where(x => !source.Any(a => keySelector(a).Equals(keySelector(x)))).ToList();
        foreach (var item in removeItems)
        {
            Entry(item).State = EntityState.Deleted;
            destination.Remove(item);
        }
        foreach (var item in source)
            if (destination.FirstOrDefault(a => keySelector(a).Equals(keySelector(item))) is var eItem && eItem is null)
            {
                Entry(item).State = EntityState.Added;
                destination.Add(item);
            }
            else
                Entry(eItem).CurrentValues.SetValues(item);
    }
}
