using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Services;

// ReSharper disable UnusedVariable

namespace Shizou;

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
        var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILogger<StartupService>>();
        log.LogInformation("Started startup service");
        var cmdMgr = scope.ServiceProvider.GetRequiredService<CommandManager>();
        var aniDbUdp = scope.ServiceProvider.GetRequiredService<AniDbUdp>();
        var processors = scope.ServiceProvider.GetServices<CommandProcessor>();
        foreach (var processor in processors) processor.Unpause();
        var importer = scope.ServiceProvider.GetRequiredService<Importer>();
        //cmdMgr.Dispatch(new HttpAnimeParams(16947));

        //var filereq = new FileRequest(_serviceProvider, 1021450, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
        //await filereq.Process();
        // var result = filereq.FileResult;

        log.LogInformation("Startup service finished");
    }
}