using Shizou.Server.Extensions.Query;

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
        var result = context.AniDbEpisodeByGenericFileId(3);
        Assert.IsNotNull(result);
    }
}
