using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Extensions;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public abstract class CommandProcessor : BackgroundService, INotifyPropertyChanged
{
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private int _commandsInQueue;
    private CommandRequest? _currentCommand;
    private bool _paused = true;
    private int _pollStep;

    private CancellationTokenSource? _wakeupTokenSource;

    protected CommandProcessor(ILogger<CommandProcessor> logger,
        QueueType queueType,
        IDbContextFactory<ShizouContext> contextFactory, IServiceScopeFactory scopeFactory)
    {
        Logger = logger;
        _contextFactory = contextFactory;
        _scopeFactory = scopeFactory;
        QueueType = queueType;
    }

    public QueueType QueueType { get; }

    private Queue<string> LastThreeCommands { get; } = new(3);

    private int PollInterval => (int)(BasePollInterval * Math.Pow((double)MaxPollInterval / BasePollInterval, (float)PollStep / MaxPollSteps));

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

    private int PollStep
    {
        get => _pollStep;
        set => _pollStep = Math.Min(value, MaxPollSteps);
    }

    protected ILogger<CommandProcessor> Logger { get; }

    protected virtual int BasePollInterval => 1000;
    protected virtual int MaxPollSteps => 4;
    protected virtual int MaxPollInterval => 10000;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public virtual void Shutdown()
    {
    }

    [SuppressMessage("ReSharper.DPA", "DPA0006: Large number of DB commands", MessageId = "count: 2000")]
    public void UpdateCommandsInQueue(ShizouContext context)
    {
        CommandsInQueue = context.CommandRequests.ByQueue(QueueType).Count();
        _wakeupTokenSource?.Cancel();
    }

    public void ClearQueue()
    {
        using var context = _contextFactory.CreateDbContext();
        context.CommandRequests.ByQueue(QueueType).ExecuteDelete();
        UpdateCommandsInQueue(context);
        Logger.LogInformation("{QueueType} queue cleared", Enum.GetName(QueueType));
    }

    public List<CommandRequest> GetQueuedCommands()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.CommandRequests.AsNoTracking().ByQueue(QueueType).ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(Shutdown);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var commandService = scope.ServiceProvider.GetRequiredService<CommandService>();
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            var context = _contextFactory.CreateDbContext();
            using (SerilogExtensions.SuppressLogging("Microsoft.EntityFrameworkCore.Database.Command"))
            {
                commandService.CreateScheduledCommands();
                UpdateCommandsInQueue(context);
            }

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

            CurrentCommand = context.CommandRequests.NextRequest(QueueType);
            Logger.LogDebug("Current command assigned");
            if (CurrentCommand is null)
                continue;
            PollStep = 0;
            var command = commandService.CommandFromRequest(CurrentCommand, scope);
            try
            {
                Logger.LogDebug("Processing command: {CommandId}", command.CommandId);
                LastThreeCommands.Enqueue(command.CommandId);
                if (LastThreeCommands.Count > 3)
                    LastThreeCommands.Dequeue();
                ProcessingCommand = true;
                var task = command.Process();
                await task.WaitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Pause(ex.Message);
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
