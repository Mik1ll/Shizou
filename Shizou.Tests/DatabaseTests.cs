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

        var and1 = new AndAllCriterion(new List<AnimeCriterion>
        {
            new AirDateCriterion(false, AirDateCriterionType.Before, 2000),
            new AirDateCriterion(false, Year: 2005, Month: 5, AirDateCriterionType: AirDateCriterionType.OnOrAfter)
        });

        var and2 = new AndAllCriterion(new List<AnimeCriterion>
        {
            new AirDateCriterion(true, Year: 1995, AirDateCriterionType: AirDateCriterionType.Before),
            new AirDateCriterion(true, Year: 2005, Month: 12, AirDateCriterionType: AirDateCriterionType.OnOrAfter)
        });
        AnimeCriterion orAny = new OrAnyCriterion(new List<AnimeCriterion>
        {
            and1, and2
        });
        var res = context.AniDbAnimes.Where(orAny.Criterion);

        var serializationOpts = new JsonSerializerOptions { TypeInfoResolver = new PolymorphicJsonTypeResolver<AnimeCriterion>() };
        var serialized = JsonSerializer.Serialize(orAny, serializationOpts);
        var deserialized = JsonSerializer.Deserialize<AnimeCriterion>(serialized, serializationOpts);

        context.AnimeFilters.Add(new AnimeFilter { Name = "Test Filter", Criteria = orAny });
        context.SaveChanges();
    }
}
