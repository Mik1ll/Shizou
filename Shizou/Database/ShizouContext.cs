using System.IO;
using Microsoft.EntityFrameworkCore;
using Shizou.Entities;

namespace Shizou.Database
{
    public sealed class ShizouContext : DbContext
    {
        public ShizouContext(DbContextOptions<ShizouContext> options) : base(options)
        {
        }

        public DbSet<CommandRequest> CommandRequests { get; set; } = null!;

        public DbSet<ImportFolder> ImportFolders { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@$"Data Source={Path.Combine(Program.ApplicationData, "ShizouDB.sqlite3")};Foreign Keys=True;");
        }
    }
}
