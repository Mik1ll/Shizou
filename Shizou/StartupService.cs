using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;
using Shizou.Commands;

namespace Shizou
{
    public sealed class StartupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var cmdMgr = scope.ServiceProvider.GetRequiredService<CommandManager>();
            var log = scope.ServiceProvider.GetRequiredService<ILogger<StartupService>>();
            var aniDbUdp = scope.ServiceProvider.GetRequiredService<AniDbUdp>();
            scope.ServiceProvider.GetRequiredService<AniDbUdpProcessor>().Paused = false;
            // Task[] tasks = new[]
            // {
            //     ActivatorUtilities.CreateInstance<PingRequest>(scope.ServiceProvider).Process(),
            //     ActivatorUtilities.CreateInstance<PingRequest>(scope.ServiceProvider).Process(),
            //     ActivatorUtilities.CreateInstance<PingRequest>(scope.ServiceProvider).Process(),
            // };
            // Task.WaitAll(tasks);
            await aniDbUdp.Login();
            //await Task.Delay(TimeSpan.FromSeconds(30));
            //await aniDbUdp.Logout();
        }
    }
}
