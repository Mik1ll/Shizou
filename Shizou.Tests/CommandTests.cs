using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.FileCaches;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Tests;

[TestClass]
public class CommandTests
{
    [TestMethod]
    // ReSharper disable once InconsistentNaming
    public void TestDI()
    {
        var provider = new ServiceCollection()
            .AddScoped<HttpAnimeResultCache>()
            .AddScoped<ImageService>()
            .AddScoped<MyAnimeListService>()
            .AddHttpClient()
            .AddOptions<ShizouOptions>().Services
            .AddScoped<CommandService>()
            .AddTransient<AnimeCommand>()
            .AddDbContextFactory<ShizouContext>()
            .AddTransient<IAnimeRequest>(_ => Mock.Of<IAnimeRequest>())
            .AddLogging()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AnimeCommand>();
    }

    [TestMethod]
    public void TestAnimeTitles()
    {
        var clientFact = Mock.Of<IHttpClientFactory>(c => c.CreateClient(It.IsAny<string>()) == new HttpClient());
        var dbcontextfact = Mock.Of<IDbContextFactory<ShizouContext>>(c => c.CreateDbContext() == new ShizouContext());
        var service = new AnimeTitleSearchService(Mock.Of<ILogger<AnimeTitleSearchService>>(), clientFact, dbcontextfact);
        _ = service.Search("Appleseed").Result;
    }
}
