using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shizou
{
    public sealed class StartupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            // // Testing code
            // var cmdMgr = scope.ServiceProvider.GetRequiredService<CommandManager>();
            // using var context = new ShizouContext();
            // try
            // {
            //     cmdMgr.Dispatch(new NoopParams {Testint = 55});
            // }
            // catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") ?? false)
            // {
            // }
            // var test = cmdMgr.CommandFromRequest(context.CommandRequests.GetNextRequest(QueueType.General)!);
            return Task.CompletedTask;
        }
    }
}
