using Microsoft.Extensions.DependencyInjection;
using Shizou.Data.Database;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.FileCaches;

namespace Shizou.Tests;

[TestClass]
public class CommandTests
{
    private readonly IServiceProvider _serviceProvider;

    public CommandTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<ShizouContext>();
        serviceCollection.AddScoped<HttpAnimeResultCache>();
        serviceCollection.AddLogging();
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [TestMethod]
    // ReSharper disable once InconsistentNaming
    public void TestDI()
    {
        using var scope = _serviceProvider.CreateScope();
        ActivatorUtilities.CreateInstance(scope.ServiceProvider, typeof(AnimeCommand), new AnimeArgs(5));
    }
}
