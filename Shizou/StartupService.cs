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
            // var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
            // try
            // {
            //     context.CommandRequests.Add(new NoopCommand(new NoopParams {Testint = 5}).CommandRequest);
            //     context.SaveChanges();
            // }
            // catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") ?? false)
            // {
            // }
            // var test = context.CommandRequests.GetNextCommand(QueueType.General);
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
