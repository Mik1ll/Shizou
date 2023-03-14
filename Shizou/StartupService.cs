﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Database;

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
        
        var processors = scope.ServiceProvider.GetServices<CommandProcessor>();
        foreach (var processor in processors) processor.Unpause();
        
        log.LogInformation("Startup service finished");
    }
}