using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands;
using Shizou.Server.Extensions.Query;

namespace Shizou.Server.CommandProcessors;

public abstract class CommandProcessor : BackgroundService, INotifyPropertyChanged
{
    private static readonly Dictionary<Type, Type> ArgsToCommandType =
        (from type in Assembly.GetAssembly(typeof(Command<CommandArgs>))!.GetTypes()
            where type.BaseType is not null && type.BaseType.IsGenericType &&
                  type.BaseType.GetGenericTypeDefinition().IsAssignableTo(typeof(ICommand<CommandArgs>))
            let argsType = type.BaseType!.GetGenericArguments()[0]
            select (argsType, type)).ToDictionary(x => x.argsType, x => x.type);

    private readonly IShizouContextFactory _contextFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private ICommand<CommandArgs>? _currentCommand;
    private bool _paused = true;
    private int _pollStep;
    private CancellationTokenSource? _wakeupTokenSource;
    private CommandRequest? _currentCommandRequest;

    protected CommandProcessor(ILogger<CommandProcessor> logger,
        QueueType queueType,
        IShizouContextFactory contextFactory,
        IServiceScopeFactory scopeFactory)
    {
        Logger = logger;
        _contextFactory = contextFactory;
        _scopeFactory = scopeFactory;
        QueueType = queueType;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public abstract string DisplayName { get; }

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

    public int CommandsInQueue => CommandQueue.Count;

    public List<string> CommandHistory { get; private set; } = [];

    public List<string> NextThreeCommands
    {
        get
        {
            lock (((ICollection)CommandQueue).SyncRoot)
            {
                return CommandQueue.Where(c => c.Value != _currentCommandRequest).Take(3).Select(c => c.Value.CommandId).ToList();
            }
        }
    }

    protected ILogger<CommandProcessor> Logger { get; }

    private SortedList<(CommandPriority Priority, int Id), CommandRequest> CommandQueue { get; } = new();

    private int PollInterval => (int)(BasePollInterval * Math.Pow((double)MaxPollInterval / BasePollInterval, (float)PollStep / MaxPollSteps));

    private int PollStep
    {
        get => _pollStep;
        set => _pollStep = Math.Min(value, MaxPollSteps);
    }

    private int BasePollInterval => 1000;
    private int MaxPollSteps => 4;
    private int MaxPollInterval => 10000;

    private static (CommandPriority Priority, int Id) GetQueueKey(CommandRequest cmdRequest) => (cmdRequest.Priority, cmdRequest.Id);

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

    public void QueueCommands(IEnumerable<CommandRequest> cmdRequests)
    {
        using var context = _contextFactory.CreateDbContext();
        using var trans = context.Database.BeginTransaction();
        foreach (var cmdRequest in cmdRequests)
        {
            if (context.CommandRequests.Any(cr => cr.CommandId == cmdRequest.CommandId))
            {
                Logger.LogInformation("Command {CommandId} already queued", cmdRequest.CommandId);
                continue;
            }

            context.CommandRequests.Add(cmdRequest);
            context.SaveChanges();
            lock (((ICollection)CommandQueue).SyncRoot)
            {
                CommandQueue.Add(GetQueueKey(cmdRequest), cmdRequest);
            }

            Logger.LogInformation("Command {CommandId} queued", cmdRequest.CommandId);
        }

        trans.Commit();
        OnPropertyChanged(nameof(CommandsInQueue));
        WakeUp();
    }

    public void ClearQueue()
    {
        using var context = _contextFactory.CreateDbContext();
        context.CommandRequests.ByQueue(QueueType).ExecuteDelete();
        lock (((ICollection)CommandQueue).SyncRoot)
        {
            CommandQueue.Clear();
        }

        _currentCommandRequest = null;
        OnPropertyChanged(nameof(CommandsInQueue));
        Logger.LogInformation("{QueueType} queue cleared", Enum.GetName(QueueType));
    }

    public List<CommandRequest> GetQueuedCommands()
    {
        lock (((ICollection)CommandQueue).SyncRoot)
        {
            return CommandQueue.Values.ToList();
        }
    }

    protected virtual Task OnShutdownAsync() => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var initContext = _contextFactory.CreateDbContext())
        {
            foreach (var cmd in initContext.CommandRequests.AsNoTracking().ByQueueOrdered(QueueType).ToList())
                lock (((ICollection)CommandQueue).SyncRoot)
                {
                    CommandQueue.Add(GetQueueKey(cmd), cmd);
                }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
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

            _currentCommandRequest = CommandQueue.FirstOrDefault().Value;
            if (_currentCommandRequest is null)
                continue;
            PollStep = 0;
            CommandHistory.Add(_currentCommandRequest.CommandId);
            CommandHistory = CommandHistory.TakeLast(100).ToList();
            using var scope = _scopeFactory.CreateScope();
            var args = _currentCommandRequest.CommandArgs;
            CurrentCommand = ((ICommand<CommandArgs>)scope.ServiceProvider.GetRequiredService(ArgsToCommandType[args.GetType()])).SetParameters(args);
            try
            {
                Logger.LogDebug("Processing command: {CommandId}", CurrentCommand.CommandId);
                await CurrentCommand.ProcessAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Pause(ex.Message);
                Logger.LogError(ex, "Error while processing command: {ExMessage}", ex.Message);
            }

            if (CurrentCommand.Completed)
            {
                DeleteCurrentCommandRequest();
            }
            else
            {
                Logger.LogWarning("Not deleting uncompleted command request: {CommandId}", CurrentCommand.CommandId);
                if (CommandHistory.Count >= 3 && CommandHistory.TakeLast(3).Distinct().Count() == 1)
                {
                    Pause($"Failed to complete command: {CurrentCommand.CommandId} after three attempts");
                    Logger.LogWarning("Queue paused after failing to complete command three times: {CommandId}", CurrentCommand.CommandId);
                }
            }
        }

        Logger.LogDebug("Processor shutting down");
        await OnShutdownAsync().ConfigureAwait(false);
    }

    private void DeleteCurrentCommandRequest()
    {
        using var context = _contextFactory.CreateDbContext();
        if (_currentCommandRequest is not null)
        {
            context.CommandRequests.Where(cr => cr.Id == _currentCommandRequest.Id).ExecuteDelete();
            lock (((ICollection)CommandQueue).SyncRoot)
            {
                CommandQueue.Remove(GetQueueKey(_currentCommandRequest));
            }

            OnPropertyChanged(nameof(CommandsInQueue));
            Logger.LogDebug("Deleted command request: {CommandId}", _currentCommandRequest.CommandId);
        }
        else
        {
            Logger.LogDebug("Not deleting command request, already deleted");
        }
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
