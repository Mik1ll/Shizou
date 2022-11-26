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
        }

        public void ReplaceNavigationCollection<T>(ICollection<T> newCollection, ICollection<T> oldCollection) where T : IEntity
        {
            foreach (var item in newCollection)
                if (oldCollection.FirstOrDefault(a => a.Id == item.Id) is var eItem && eItem is null)
                    oldCollection.Add(item);
                else
                    Entry(eItem).CurrentValues.SetValues(item);
            foreach (var item in newCollection)
                if (!newCollection.Any(a => a.Id == item.Id))
                    Remove(item);
        }
    }
}
