using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Exceptions;
using Shizou.Extensions;
using Shizou.Services;
using ShizouData.Database;
using ShizouData.Enums;
using ShizouData.Models;

namespace Shizou.CommandProcessors;

public abstract class CommandProcessor : BackgroundService
{
    protected readonly IServiceProvider Provider;
    public readonly QueueType QueueType;

    protected CommandProcessor(ILogger<CommandProcessor> logger, IServiceProvider provider, QueueType queueType)
    {
        Logger = logger;
        Provider = provider;
        QueueType = queueType;
        PollInterval = BasePollInterval;
    }

    protected ILogger<CommandProcessor> Logger { get; }

    public bool ProcessingCommand { get; private set; }

    public CommandRequest? CurrentCommand { get; private set; }

    public int CommandsInQueue { get; private set; }

    public virtual bool Paused { get; protected set; } = true;
    public virtual string? PauseReason { get; protected set; }

    private CancellationTokenSource? _unpauseTokenSource;

    public void Pause(string? pauseReason = null)
    {
        Paused = true;
        PauseReason = pauseReason;
        if (pauseReason is null)
            Logger.LogInformation("Processor paused");
        else
            Logger.LogInformation("Processor paused with reason: {PauseReason}", pauseReason);
    }

    public void Unpause()
    {
        Paused = false;
        if (!Paused)
        {
            _unpauseTokenSource?.Cancel();
            PauseReason = null;
            Logger.LogInformation("Processor unpaused");
        }
    }

    public Queue<string> LastThreeCommands { get; set; } = new(3);

    protected int BasePollInterval { get; set; } = 1000;
    protected int PollInterval { get; set; }

    public virtual void Shutdown()
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(Shutdown);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = Provider.CreateScope();
            var commandManager = scope.ServiceProvider.GetRequiredService<CommandService>();
            var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
            CommandsInQueue = context.CommandRequests.GetQueueCount(QueueType);
            if (Paused || CommandsInQueue == 0)
            {
                _unpauseTokenSource = new CancellationTokenSource();
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_unpauseTokenSource.Token, stoppingToken);
                try
                {
                    await Task.Delay(PollInterval, linkedTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                }
                finally
                {
                    PollInterval = Math.Min((int)(PollInterval * Math.Pow(10, 1f / 4)), 10000);
                    _unpauseTokenSource.Dispose();
                    linkedTokenSource.Dispose();
                }
                continue;
            }
            CurrentCommand = context.CommandRequests.GetNextRequest(QueueType);
            if (CurrentCommand is null)
                continue;
            PollInterval = BasePollInterval;
            var command = commandManager.CommandFromRequest(CurrentCommand);
            try
            {
                Logger.LogDebug("Processing command: {CommandId}", command.CommandId);
                LastThreeCommands.Enqueue(command.CommandId);
                if (LastThreeCommands.Count > 3)
                    LastThreeCommands.Dequeue();
                ProcessingCommand = true;
                var task = command.Process();
                while (!stoppingToken.IsCancellationRequested && !task.IsCompleted)
                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(500);
            }
            catch (ProcessorPauseException ex)
            {
                Pause(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while processing command: {ExMessage}", ex.Message);
            }
            finally
            {
                ProcessingCommand = false;
            }

            if (command.Completed)
            {
                Logger.LogDebug("Deleting command: {CommandId}", command.CommandId);
                context.CommandRequests.Remove(CurrentCommand);
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                context.SaveChanges();
            }
            else
            {
                Logger.LogWarning("Not deleting uncompleted command: {CommandId}", command.CommandId);
                if (LastThreeCommands.Count >= 3 && LastThreeCommands.Distinct().Count() == 1)
                {
                    Pause($"Failed to complete command: {command.CommandId} after three attempts");
                    Logger.LogWarning("Queue paused after failing to complete command three times: {CommandId}", command.CommandId);
                }
            }
            CurrentCommand = null;
        }
    }
}
