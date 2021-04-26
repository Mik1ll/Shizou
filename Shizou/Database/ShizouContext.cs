using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Entities;

namespace Shizou.Database
{
    public sealed class ShizouContext : DbContext
    {
        private readonly ILoggerFactory _loggerFactory;

        public ShizouContext(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public ShizouContext(DbContextOptions<ShizouContext> options, ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory;
        }

        public DbSet<CommandRequest> CommandRequests { get; set; } = null!;

        public DbSet<ImportFolder> ImportFolders { get; set; } = null!;

        public DbSet<AniDbAnime> AniDbAnimes { get; set; } = null!;

        public DbSet<AniDbEpisode> AniDbEpisodes { get; set; } = null!;

        public DbSet<AniDbFile> AniDbFiles { get; set; } = null!;

        public DbSet<AniDbGroup> AniDbGroups { get; set; } = null!;


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@$"Data Source={Path.Combine(Program.ApplicationData, "ShizouDB.sqlite3")};Foreign Keys=True;")
                .EnableSensitiveDataLogging().UseLoggerFactory(_loggerFactory);
        }
    }
}
