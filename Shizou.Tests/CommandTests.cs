using Microsoft.Extensions.DependencyInjection;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Http;
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
            .AddScoped<HttpRequestFactory>()
            .AddScoped<ImageService>()
            .AddScoped<MyAnimeListService>()
            .AddHttpClient()
            .AddOptions<ShizouOptions>().Services
            .AddScoped<CommandService>()
            .AddTransient<AnimeCommand>()
            .AddDbContextFactory<ShizouContext>()
            .AddLogging()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AnimeCommand>();
    }
}
