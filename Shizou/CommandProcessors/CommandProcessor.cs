using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Entities;
using Shizou.Extensions;

namespace Shizou.CommandProcessors
{
    public abstract class CommandProcessor : BackgroundService
    {
        protected readonly IServiceProvider Provider;
        public readonly QueueType QueueType;

        protected CommandProcessor(ILogger<CommandProcessor> logger, IServiceProvider provider, QueueType queueType)
        {
            Logger = logger;
            Provider = provider;
            QueueType = queueType;
        }

        protected ILogger<CommandProcessor> Logger { get; }

        public bool ProcessingCommand { get; protected set; }

        public CommandRequest? CurrentCommand { get; protected set; }

        public virtual bool Paused { get; protected set; } = true;
        public virtual string? PauseReason { get; protected set; }

        public void Pause(string? pauseReason = null)
        {
            Paused = true;
            PauseReason = pauseReason;
        }

        public void Unpause()
        {
            Paused = false;
            if (!Paused)
                PauseReason = null;
        }

        public Queue<string> LastThreeCommands { get; set; } = new(3);

        protected int PollInterval { get; set; } = 1000;

        public virtual void Shutdown()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(Shutdown);
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = Provider.CreateScope();
                CommandManager commandManager = scope.ServiceProvider.GetRequiredService<CommandManager>();
                ShizouContext context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
                if (Paused || (CurrentCommand = context.CommandRequests.GetNextRequest(QueueType)) is null)
                {
                    await Task.Delay(PollInterval);
                    continue;
                }
                ICommand command = commandManager.CommandFromRequest(CurrentCommand);
                try
                {
                    Logger.LogDebug("Processing command: {commandId}", command.CommandId);
                    LastThreeCommands.Enqueue(command.CommandId);
                    if (LastThreeCommands.Count > 3)
                        LastThreeCommands.Dequeue();
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
                    if (LastThreeCommands.Count >= 3 && LastThreeCommands.Distinct().Count() == 1)
                    {
                        Pause($"Failed to complete command: {command.CommandId} after three attempts");
                        Logger.LogWarning("Queue paused after failing to complete command three times: {commandId}", command.CommandId);
                    }
                }
                CurrentCommand = null;
            }
        }
    }
}
