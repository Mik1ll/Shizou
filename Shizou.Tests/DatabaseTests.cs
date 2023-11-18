using Shizou.Data.Filters;

namespace Shizou.Tests;

[TestClass]
public class DatabaseTests : SeededDatabaseTests
{
    [TestMethod]
    public void TestDateTimeConversion()
    {
        using var context = GetContext();
        var result = context.AniDbAnimes.Where(a => a.Updated < DateTime.UtcNow).ToList();
        Assert.IsNotNull(result);
    }


    [TestMethod]
    public void TestQueryables()
    {
        using var context = GetContext();
        var result = from file in context.AniDbFiles
            where file.LocalFile != null && file.AniDbEpisodes.Any(ep => ep.AniDbAnimeId == 5)
            select file;
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TestFilters()
    {
        using var context = GetContext();
        var filters = new[]
        {
            new AirDateFilter { Year = 2000, AirDateCriteria = AirDateCriteria.Before },
            new AirDateFilter { Year = 2005, Month = 5, AirDateCriteria = AirDateCriteria.OnOrAfter }
        };

        var filters2 = new[]
        {
            new AirDateFilter { Year = 1995, AirDateCriteria = AirDateCriteria.Before },
            new AirDateFilter { Year = 2005, Month = 12, AirDateCriteria = AirDateCriteria.OnOrAfter }
        };
        var res1 = filters.Aggregate(context.AniDbAnimes.AsQueryable(), (animes, filter) => animes.Where(filter.AnimeFilter));
        var res2 = filters2.Aggregate(context.AniDbAnimes.AsQueryable(), (animes, filter) => animes.Where(filter.AnimeFilter));
        var res3 = res1.Union(res2);
        ;
    }
}
