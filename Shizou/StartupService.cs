using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Services.Import;

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
            await Task.Yield();
            using IServiceScope scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
            await context.Database.MigrateAsync();
            var log = scope.ServiceProvider.GetRequiredService<ILogger<StartupService>>();
            log.LogInformation("Started background startup service");
            var cmdMgr = scope.ServiceProvider.GetRequiredService<CommandManager>();
            var aniDbUdp = scope.ServiceProvider.GetRequiredService<AniDbUdp>();
            var processors = scope.ServiceProvider.GetServices<CommandProcessor>();
            foreach (var processor in processors) processor.Paused = false;
            var importer = scope.ServiceProvider.GetRequiredService<Importer>();
            
        }
    }
}
