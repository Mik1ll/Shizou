using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.Commands.AniDb;
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
            .AddScoped<ImageService>()
            .AddScoped<MyAnimeListService>()
            .AddHttpClient()
            .AddOptions<ShizouOptions>().Services
            .AddScoped<CommandService>()
            .AddTransient<AnimeCommand>()
            .AddTransient<FfmpegService>()
            .AddDbContextFactory<ShizouContext>()
            .AddTransient<LinkGenerator>(_ => Mock.Of<LinkGenerator>())
            .AddScoped<IShizouContext, ShizouContext>(p => p.GetRequiredService<ShizouContext>())
            .AddSingleton<IShizouContextFactory, ShizouContextFactory>(p =>
                new ShizouContextFactory(p.GetRequiredService<IDbContextFactory<ShizouContext>>()))
            .AddTransient<IAnimeRequest>(_ => Mock.Of<IAnimeRequest>())
            .AddLogging()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AnimeCommand>();
    }
}
