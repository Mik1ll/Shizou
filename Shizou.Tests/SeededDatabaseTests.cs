using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
                using var scope = GetServiceCollection().BuildServiceProvider().CreateScope();
                using var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
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

    protected static ServiceCollection GetServiceCollection()
    {
        var collection = new ServiceCollection();
        collection.AddDbContextFactory<ShizouContext>(opts => opts.UseSqlite(ConnectionString));
        collection.AddLogging();
        return collection;
    }
}