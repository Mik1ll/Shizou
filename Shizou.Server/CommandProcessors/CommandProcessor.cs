using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands;
using Shizou.Server.Extensions;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public abstract class CommandProcessor : BackgroundService, INotifyPropertyChanged
{
    private readonly Func<CommandService> _commandServiceFactory;
    private readonly IShizouContextFactory _contextFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private int _commandsInQueue;
    private ICommand<CommandArgs>? _currentCommand;
    private bool _paused = true;
    private int _pollStep;
    private CancellationTokenSource? _wakeupTokenSource;

    protected CommandProcessor(ILogger<CommandProcessor> logger,
        QueueType queueType,
        IShizouContextFactory contextFactory,
        IServiceScopeFactory scopeFactory,
        Func<CommandService> commandServiceFactory)
    {
        Logger = logger;
        _contextFactory = contextFactory;
        _scopeFactory = scopeFactory;
        _commandServiceFactory = commandServiceFactory;
        QueueType = queueType;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual bool Paused
    {
        get => _paused;
        protected set
        {
            SetField(ref _paused, value);
            if (value)
            {
                if (PauseReason is null)
                    Logger.LogInformation("Processor paused");
                else
                    Logger.LogInformation("Processor paused with reason: {PauseReason}", PauseReason);
            }
            else
            {
                PauseReason = null;
                Logger.LogInformation("Processor unpaused");
                WakeUp();
            }
        }
    }

    public virtual string? PauseReason { get; protected set; }

    public QueueType QueueType { get; }

    public ICommand<CommandArgs>? CurrentCommand
    {
        get => _currentCommand;
        private set => SetField(ref _currentCommand, value);
    }

    public int CommandsInQueue
    {
        get => _commandsInQueue;
        private set => SetField(ref _commandsInQueue, value);
    }

    protected ILogger<CommandProcessor> Logger { get; }

    private CommandRequest? CurrentCommandRequest { get; set; }

    private Queue<string> LastThreeCommands { get; } = new(3);

    private int PollInterval => (int)(BasePollInterval * Math.Pow((double)MaxPollInterval / BasePollInterval, (float)PollStep / MaxPollSteps));

    private int PollStep
    {
        get => _pollStep;
        set => _pollStep = Math.Min(value, MaxPollSteps);
    }

    private int BasePollInterval => 1000;
    private int MaxPollSteps => 4;
    private int MaxPollInterval => 10000;

    public void Pause(string? pauseReason = null)
    {
        PauseReason = pauseReason;
        Paused = true;
    }

    public bool Unpause()
    {
        Paused = false;
        return !Paused;
    }

    public void QueueCommand(CommandRequest cmdRequest)
    {
        using var context = _contextFactory.CreateDbContext();
        if (!context.CommandRequests.Any(cr => cr.CommandId == cmdRequest.CommandId))
        {
            context.CommandRequests.Add(cmdRequest);
            context.SaveChanges();
            CommandsInQueue++;
            WakeUp();
            Logger.LogInformation("Command {CommandId} queued", cmdRequest.CommandId);
        }
        else
        {
            Logger.LogInformation("Command {CommandId} already queued", cmdRequest.CommandId);
        }
    }

    public void ClearQueue()
    {
        using var context = _contextFactory.CreateDbContext();
        context.CommandRequests.ByQueue(QueueType).ExecuteDelete();
        CommandsInQueue = 0;
        CurrentCommandRequest = null;
        Logger.LogInformation("{QueueType} queue cleared", Enum.GetName(QueueType));
    }

    public List<CommandRequest> GetQueuedCommands()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.CommandRequests.ByQueueOrdered(QueueType).AsNoTracking().ToList();
    }

    protected virtual Task OnShutdownAsync() => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var context = _contextFactory.CreateDbContext();
            var commandService = _commandServiceFactory();
            using (SerilogExtensions.SuppressLogging("Microsoft.EntityFrameworkCore.Database.Command"))
            {
                commandService.CreateScheduledCommands(QueueType);
                UpdateCommandsInQueue(context);
            }

            if (Paused || CommandsInQueue == 0)
            {
                CurrentCommand = null;
                _wakeupTokenSource = new CancellationTokenSource();
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_wakeupTokenSource.Token, stoppingToken);
                try
                {
                    await Task.Delay(PollInterval, linkedTokenSource.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    if (stoppingToken.IsCancellationRequested)
                        continue;
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

            CurrentCommandRequest = context.CommandRequests.Next(QueueType);
            if (CurrentCommandRequest is null)
                continue;
            PollStep = 0;
            using var scope = _scopeFactory.CreateScope();
            var args = JsonSerializer.Deserialize(CurrentCommandRequest.CommandArgs, CommandArgs.GetJsonTypeInfo())!;
            var cmd = (ICommand<CommandArgs>)scope.ServiceProvider.GetRequiredService(args.CommandType);
            cmd.SetParameters(args);
            CurrentCommand = cmd;
            try
            {
                Logger.LogDebug("Processing command: {CommandId}", CurrentCommand.CommandId);
                LastThreeCommands.Enqueue(CurrentCommand.CommandId);
                while (LastThreeCommands.Count > 3)
                    LastThreeCommands.Dequeue();
                await CurrentCommand.ProcessAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Pause(ex.Message);
                Logger.LogError(ex, "Error while processing command: {ExMessage}", ex.Message);
            }

            if (CurrentCommand.Completed)
            {
                if (CurrentCommandRequest is not null)
                {
                    Logger.LogDebug("Deleting command request: {CommandId}", CurrentCommand.CommandId);
                    context.CommandRequests.Remove(CurrentCommandRequest);
                    using (SerilogExtensions.SuppressLogging("Microsoft.EntityFrameworkCore.Database.Command"))
                    {
                        context.SaveChanges();
                    }
                }
                else
                {
                    Logger.LogDebug("Not deleting command request, already deleted");
                }
            }
            else
            {
                Logger.LogWarning("Not deleting uncompleted command request: {CommandId}", CurrentCommand.CommandId);
                if (LastThreeCommands.Count >= 3 && LastThreeCommands.Distinct().Count() == 1)
                {
                    Pause($"Failed to complete command: {CurrentCommand.CommandId} after three attempts");
                    Logger.LogWarning("Queue paused after failing to complete command three times: {CommandId}", CurrentCommand.CommandId);
                }
            }
        }

        Logger.LogDebug("Processor shutting down");
        await OnShutdownAsync().ConfigureAwait(false);
    }

    private void WakeUp()
    {
        try
        {
            _wakeupTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            _wakeupTokenSource = null;
        }
    }

    [SuppressMessage("ReSharper.DPA", "DPA0006: Large number of DB commands", MessageId = "count: 2000")]
    private void UpdateCommandsInQueue(IShizouContext context)
    {
        CommandsInQueue = context.CommandRequests.ByQueue(QueueType).Count();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
