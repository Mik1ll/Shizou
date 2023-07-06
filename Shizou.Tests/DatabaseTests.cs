using Shizou.Data.Database;

namespace Shizou.Tests;

[TestClass]
public class DatabaseTests
{
    [TestMethod]
    public void TestDateTimeConversion()
    {
        using var context = new ShizouContext();
        var result = context.AniDbAnimes.Where(a => a.Updated < DateTime.UtcNow).ToList();
        Assert.IsNotNull(result);
    }
}
