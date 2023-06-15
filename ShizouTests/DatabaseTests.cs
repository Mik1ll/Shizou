using Microsoft.EntityFrameworkCore;
using ShizouData.Database;

namespace ShizouTests;

[TestClass]
public class DatabaseTests
{
    [TestMethod]
    public void TestDateTimeConversion()
    {
        using var context = new ShizouContext();
        var anime = context.AniDbAnimes.Where(a => a.Updated < DateTime.UtcNow);
        var query = anime.ToQueryString();
        var result = anime.ToList();
    }
}
