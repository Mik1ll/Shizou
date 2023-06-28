﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Common.Enums;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Exceptions;
using Shizou.Server.Extensions;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public abstract class CommandProcessor : BackgroundService, INotifyPropertyChanged
{
    protected readonly IServiceProvider Provider;

    private CancellationTokenSource? _wakeupTokenSource;
    private CommandRequest? _currentCommand;
    private int _commandsInQueue;
    private bool _paused = true;
    private int _pollStep;

    protected CommandProcessor(ILogger<CommandProcessor> logger, IServiceProvider provider, QueueType queueType)
    {
        Logger = logger;
        Provider = provider;
        QueueType = queueType;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public QueueType QueueType { get; }

    public Queue<string> LastThreeCommands { get; } = new(3);

    public int PollInterval => (int)(BasePollInterval * Math.Pow((double)MaxPollInterval / BasePollInterval, (float)PollStep / MaxPollSteps));

    public bool ProcessingCommand { get; private set; }

    public CommandRequest? CurrentCommand
    {
        get => _currentCommand;
        private set => SetField(ref _currentCommand, value);
    }

    public int CommandsInQueue
    {
        get => _commandsInQueue;
        private set => SetField(ref _commandsInQueue, value);
    }

    public virtual bool Paused
    {
        get => _paused;
        protected set
        {
            if (value)
            {
                if (PauseReason is null)
                    Logger.LogInformation("Processor paused");
                else
                    Logger.LogInformation("Processor paused with reason: {PauseReason}", PauseReason);
            }
            else
            {
                _wakeupTokenSource?.Cancel();
                PauseReason = null;
                Logger.LogInformation("Processor unpaused");
            }
            SetField(ref _paused, value);
        }
    }

    public virtual string? PauseReason { get; protected set; }

    public int PollStep
    {
        get => _pollStep;
        private set => _pollStep = Math.Min(value, MaxPollSteps);
    }

    protected ILogger<CommandProcessor> Logger { get; }

    protected virtual int BasePollInterval => 1000;
    protected virtual int MaxPollSteps => 4;
    protected virtual int MaxPollInterval => 10000;

    public void Pause(string? pauseReason = null)
    {
        PauseReason = pauseReason;
        Paused = true;
    }

    public void Unpause()
    {
        Paused = false;
    }

    public virtual void Shutdown()
    {
    }

    public void UpdateCommandsInQueue()
    {
        using var scope = Provider.CreateScope();
        CommandsInQueue = scope.ServiceProvider.GetRequiredService<ShizouContext>().CommandRequests.GetQueueCount(QueueType);
        _wakeupTokenSource?.Cancel();
    }

    public void ClearQueue()
    {
        using var scope = Provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
        context.CommandRequests.ClearQueue(QueueType);
        context.SaveChanges();
        UpdateCommandsInQueue();
        Logger.LogInformation("{QueueType} queue cleared", Enum.GetName(QueueType));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(Shutdown);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = Provider.CreateScope();
            var commandManager = scope.ServiceProvider.GetRequiredService<CommandService>();
            var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();
            UpdateCommandsInQueue();
            if (Paused || CommandsInQueue == 0)
            {
                _wakeupTokenSource = new CancellationTokenSource();
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_wakeupTokenSource.Token, stoppingToken);
                try
                {
                    await Task.Delay(PollInterval, linkedTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    Logger.LogDebug("Processor woken up from pause/inactive state");
                }
                finally
                {
                    PollStep++;
                    _wakeupTokenSource.Dispose();
                    _wakeupTokenSource = null;
                    linkedTokenSource.Dispose();
                }
                continue;
            }
            CurrentCommand = context.CommandRequests.GetNextRequest(QueueType);
            Logger.LogDebug("Current command assigned");
            if (CurrentCommand is null)
                continue;
            PollStep = 0;
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

                try
                {
                    if (context.CommandRequests.Any(cr => cr.Id == CurrentCommand.Id))
                    {
                        context.CommandRequests.Remove(CurrentCommand);
                        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                        context.SaveChanges();
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    ex.Entries.Single().State = EntityState.Detached;
                }
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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}