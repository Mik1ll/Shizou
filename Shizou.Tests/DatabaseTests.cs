using Microsoft.Extensions.DependencyInjection;
using Shizou.Data.Database;

namespace Shizou.Tests;

[TestClass]
public class DatabaseTests : SeededDatabaseTests
{
    [TestMethod]
    public void TestDateTimeConversion()
    {
        var provider = GetServiceCollection().BuildServiceProvider();
        using var context = provider.GetRequiredService<ShizouContext>();
        var result = context.AniDbAnimes.Where(a => a.Updated < DateTime.UtcNow).ToList();
        Assert.IsNotNull(result);
    }
}
