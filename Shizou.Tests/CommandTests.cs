using Microsoft.Extensions.DependencyInjection;
using Shizou.Data.Database;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.FileCaches;

namespace Shizou.Tests;

[TestClass]
public class CommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceCollection _serviceCollection;

    public CommandTests()
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddDbContext<ShizouContext>();
        _serviceCollection.AddScoped<HttpAnimeResultCache>();
        _serviceCollection.AddLogging();
        _serviceProvider = _serviceCollection.BuildServiceProvider();
    }

    [TestMethod]
    // ReSharper disable once InconsistentNaming
    public void TestDI()
    {
        using var scope = _serviceProvider.CreateScope();
        var command = ActivatorUtilities.CreateInstance(scope.ServiceProvider, typeof(AnimeCommand), new AnimeArgs(5));
        ;
    }
}
