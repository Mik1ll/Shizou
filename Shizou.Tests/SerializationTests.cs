using Microsoft.Extensions.DependencyInjection;
using Shizou.Server.FileCaches;

namespace Shizou.Tests;

[TestClass]
public class SerializationTests
{
    private readonly ServiceProvider _serviceProvider;

    public SerializationTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<HttpAnimeResultCache>();
        serviceCollection.AddLogging();
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [TestMethod]
    public void TestAnimeDeserialization()
    {
        var cache = _serviceProvider.GetRequiredService<HttpAnimeResultCache>();
        var animeResult = cache.Get("AnimeDoc_12704.xml").Result;
        if (animeResult is not null)
            ;
        else
            Assert.Inconclusive("Anime xml not found");
    }
}