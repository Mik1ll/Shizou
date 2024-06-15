using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi;

namespace Shizou.Server.Services;

public sealed class StartupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public StartupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        using var scope = _serviceProvider.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILogger<StartupService>>();
        log.LogInformation("Started startup service");
        var udpState = _serviceProvider.GetRequiredService<AniDbUdpState>();
        await udpState.SetupNatAsync().ConfigureAwait(false);
        var animeTitleSearchService = _serviceProvider.GetRequiredService<IAnimeTitleSearchService>();
        animeTitleSearchService.ScheduleNextUpdate();
        
        log.LogInformation("Startup service finished");
    }
}
