using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Extensions;

namespace Shizou.CommandProcessors
{
    public class AniDbUdpProcessor : CommandProcessor
    {
        private readonly CommandManager _commandManager;
        private readonly AniDbUdp _udpApi;

        public AniDbUdpProcessor(ILogger<CommandProcessor> logger, CommandManager commandManager, AniDbUdp udpApi) : base(logger)
        {
            _udpApi = udpApi;
            _commandManager = commandManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ShizouContext context = new();
                if (Paused || _udpApi.Banned || _udpApi.Paused || (CurrentCommand = context.CommandRequests.GetNextRequest(QueueType.AniDbUdp)) is null)
                {
                    await Task.Delay(2000);
                    continue;
                }
                ICommand command = _commandManager.CommandFromRequest(CurrentCommand);
                try
                {
                    await command.Process();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while processing command: {ExMessage}", ex.Message);
                }
                if (!command.Completed)
                    Logger.LogError("Command did not complete successfully: {commandId}", command.CommandId);
                context.CommandRequests.Remove(CurrentCommand);
            }
        }
    }
}
