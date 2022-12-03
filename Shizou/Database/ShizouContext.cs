using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Models;

namespace Shizou.Database
{
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

        public IQueryable<AniDbEpisode> GetEpisodesFromFile(int fileId)
        {
            return AniDbEpisodes.Where(e => AniDbEpisodeFileXrefs.Any(r => r.AniDbFileId == fileId && r.AniDbEpisodeId == e.Id));
        }

        public IQueryable<AniDbFile> GetFilesFromEpisode(int episodeId)
        {
            return AniDbFiles.Where(f => AniDbEpisodeFileXrefs.Any(r => r.AniDbFileId == f.Id && r.AniDbEpisodeId == episodeId));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(@$"Data Source={Path.Combine(Constants.ApplicationData, "ShizouDB.sqlite3")};Foreign Keys=True;")
                .EnableSensitiveDataLogging(false)
                ;
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

        public void ReplaceNavigationList<T>(List<T> source, List<T> destination) where T : IEntity
        {
            foreach (var item in source)
                if (destination.FirstOrDefault(a => a.Id == item.Id) is var eItem && eItem is null)
                    destination.Add(item);
                else
                    Entry(eItem).CurrentValues.SetValues(item);
            destination.RemoveAll(x => !source.Any(a => a.Id == x.Id));
        }
    }
}
