using Microsoft.EntityFrameworkCore;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Tests;

[TestClass]
public class SeededDatabaseTests
{
    private static readonly string ConnectionString = $"Data Source={Path.Combine(FilePaths.ApplicationDataDir, "ShizouTestDB.sqlite3")};Foreign Keys=True;";

    private static readonly object Lock = new();
    private static bool _databaseInitialized;

    public SeededDatabaseTests()
    {
        lock (Lock)
        {
            if (!_databaseInitialized)
            {
                using var context = GetContext();
                context.Database.EnsureDeleted();
                context.Database.Migrate();
                context.AniDbAnimes.AddRange(new AniDbAnime
                {
                    Id = 1,
                    Title = "blah",
                    AnimeType = AnimeType.TvSeries,
                    EpisodeCount = 12,
                    AirDate = "2022-02-15",
                    EndDate = null,
                    Description = "testdescription",
                    Restricted = false,
                    ImageFilename = null,
                    Updated = default
                });

                context.SaveChanges();

                _databaseInitialized = true;
            }
        }
    }

    protected static ShizouContext GetContext()
    {
        return new ShizouContext(new DbContextOptionsBuilder<ShizouContext>().UseSqlite(ConnectionString).Options);
    }
}