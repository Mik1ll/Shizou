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
            where file.LocalFiles.Any() && file.AniDbEpisodes.Any(ep => ep.AniDbAnimeId == 5)
            select file;
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TestFilters()
    {
        using var context = GetContext();

        var and1 = new AndAllCriterion([
            new AirDateCriterion { AirDateTermType = AirDateTermType.AirDate, AirDateTermRange = AirDateTermRange.Before, Year = 2000 },
            new AirDateCriterion { AirDateTermType = AirDateTermType.AirDate, Year = 2005, Month = 5, AirDateTermRange = AirDateTermRange.OnOrAfter },
        ]);

        var and2 = new AndAllCriterion([
            new AirDateCriterion { Negated = true, AirDateTermType = AirDateTermType.AirDate, AirDateTermRange = AirDateTermRange.Before, Year = 1995 },
            new AirDateCriterion
                { Negated = true, AirDateTermType = AirDateTermType.AirDate, AirDateTermRange = AirDateTermRange.OnOrAfter, Year = 2005, Month = 12 },
        ]);
        var orAny = new OrAnyCriterion([and1, and2]);
        var res = context.AniDbAnimes.Where(orAny.Criterion);

        var serializationOpts = new JsonSerializerOptions { TypeInfoResolver = new PolymorphicJsonTypeResolver<TermCriterion>() };
        var serialized = JsonSerializer.Serialize(orAny, serializationOpts);
        var deserialized = JsonSerializer.Deserialize<OrAnyCriterion>(serialized, serializationOpts);

        context.AnimeFilters.Add(new AnimeFilter { Name = "Test Filter", Criteria = orAny });
        context.SaveChanges();
    }

    [TestMethod]
    public void TestJsonColumns()
    {
        using var context = GetContext();
        var file = context.AniDbNormalFiles.First();
        var audio = file.Audio;
        var subtitles = file.Subtitles;
        Assert.IsNotEmpty(audio);
        Assert.IsNotEmpty(subtitles);
    }
}
