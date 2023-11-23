using System.Linq.Expressions;
using System.Text.Json;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;
using Shizou.Data.Utilities;

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
        AnimeCriterion[] andFilters1 =
        {
            new AirDateCriterion(false, AirDateCriterionType.Before, 2000),
            new AirDateCriterion(false, year: 2005, month: 5, airDateCriterionType: AirDateCriterionType.OnOrAfter)
        };

        AnimeCriterion[] andFilters2 =
        {
            new AirDateCriterion(true, year: 1995, airDateCriterionType: AirDateCriterionType.Before),
            new AirDateCriterion(true, year: 2005, month: 12, airDateCriterionType: AirDateCriterionType.OnOrAfter)
        };
        var orFilters = new[] { andFilters1, andFilters2 };
        var serialized =
            JsonSerializer.Serialize(orFilters, new JsonSerializerOptions { TypeInfoResolver = new PolymorphicJsonTypeResolver<AnimeCriterion>() });
        var expression2 = orFilters.Select(x => x.Select(y => ParameterReplacer.Replace(y.Criterion, animeParam))
                .Aggregate(Expression.AndAlso))
            .Aggregate(Expression.OrElse);
        var lambda2 = Expression.Lambda<Func<AniDbAnime, bool>>(expression2, true, animeParam);
        var res = context.AniDbAnimes.Where(lambda2);
        _ = res.ToList();
    }
}
