using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Database;

public class AuthContext : IdentityDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(new SqliteConnectionStringBuilder { DataSource = FilePaths.IdentityDatabasePath }.ConnectionString);
    }
}
