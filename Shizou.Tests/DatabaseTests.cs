using System.Linq.Expressions;
using Shizou.Data.Filters;
using Shizou.Data.Models;

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

        var animeParam = Expression.Parameter(typeof(AniDbAnime), "anime");
        var andFilters1 = new[]
        {
            new AirDateFilter(false, AirDateCriteria.Before, 2000),
            new AirDateFilter(false, year: 2005, month: 5, airDateCriteria: AirDateCriteria.OnOrAfter)
        };

        var andFilters2 = new[]
        {
            new AirDateFilter(true, year: 1995, airDateCriteria: AirDateCriteria.Before),
            new AirDateFilter(true, year: 2005, month: 12, airDateCriteria: AirDateCriteria.OnOrAfter)
        };
        var orFilters = new[] { andFilters1, andFilters2 };
        var expression2 = orFilters.Select(x => x.Select(y => ParameterReplacer.Replace(y.Filter, animeParam))
                .Aggregate(Expression.AndAlso))
            .Aggregate(Expression.OrElse);
        var lambda2 = Expression.Lambda<Func<AniDbAnime, bool>>(expression2, true, animeParam);
        var res = context.AniDbAnimes.Where(lambda2);
        _ = res.ToList();
    }
}
