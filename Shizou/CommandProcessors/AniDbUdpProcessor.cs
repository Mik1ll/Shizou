using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Extensions;

namespace Shizou.CommandProcessors
{
    public class AniDbUdpProcessor : CommandProcessor
    {
        private readonly IServiceProvider _provider;
        private readonly AniDbUdp _udpApi;

        public AniDbUdpProcessor(ILogger<CommandProcessor> logger, AniDbUdp udpApi, IServiceProvider provider) : base(logger)
        {
            _provider = provider;
            _udpApi = udpApi;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();
                CommandManager commandManager = scope.ServiceProvider.GetRequiredService<CommandManager>();
                ShizouContext context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
                if (Paused || _udpApi.Banned || _udpApi.Paused || (CurrentCommand = context.CommandRequests.GetNextRequest(QueueType.AniDbUdp)) is null)
                {
                    await Task.Delay(1000);
                    continue;
                }
                ICommand command = commandManager.CommandFromRequest(CurrentCommand);
                try
                {
                    Logger.LogDebug("Processing command: {commandId}", command.CommandId);
                    var task = command.Process();
                    while (!stoppingToken.IsCancellationRequested && !task.IsCompleted)
                        await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while processing command: {ExMessage}", ex.Message);
                }

                if (command.Completed)
                {
                    Logger.LogDebug("Deleting command: {commandId}", command.CommandId);
                    context.CommandRequests.Remove(CurrentCommand);
                    context.SaveChanges();
                }
                else
                {
                    Logger.LogWarning("Not deleting uncompleted command: {commandId}", command.CommandId);
                }
                CurrentCommand = null;
            }
            await _udpApi.Logout();
        }
    }
}
