using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Enums;
using Shizou.Extensions;

namespace Shizou
{
    public sealed class StartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            // Testing code
            // var cmdMgr = scope.ServiceProvider.GetRequiredService<CommandManager>();
            // var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
            // try
            // {
            //     cmdMgr.Dispatch<NoopCommand>(new NoopParams {Testint = 20});
            // }
            // catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") ?? false)
            // {
            // }
            // var test = cmdMgr.CommandFromRequest(context.CommandRequests.GetNextRequest(QueueType.General)!);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
