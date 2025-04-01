using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Extensions;
using Shizou.Server.Extensions.Query;

namespace Shizou.Server.Services;

public class CommandService : BackgroundService
{
    private readonly IShizouContextFactory _contextFactory;
    private readonly ILogger<CommandService> _logger;
    private readonly List<CommandProcessor> _processors;

    public CommandService(
        ILogger<CommandService> logger,
        IShizouContextFactory contextFactory,
        IEnumerable<CommandProcessor> processors)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _processors = processors.ToList();
    }

    /// <inheritdoc cref="Dispatch{TArgs}(IEnumerable{TArgs})"/>
    public void Dispatch<TArgs>(params TArgs[] commandArgs) where TArgs : CommandArgs
    {
        Dispatch((IEnumerable<TArgs>)commandArgs);
    }

    /// <summary>
    ///     Queues commands given the corresponding arguments
    /// </summary>
    /// <param name="commandArgs">The argument objects corresponding to the desired commands</param>
    /// <typeparam name="TArgs">The type of the argument objects, must be <see cref="CommandArgs" /> or derived</typeparam>
    public void Dispatch<TArgs>(IEnumerable<TArgs> commandArgs) where TArgs : CommandArgs
    {
        var grouped = commandArgs.Select(a => a.CommandRequest).GroupBy(a => a.QueueType);
        foreach (var group in grouped)
            _processors.Single(p => p.QueueType == group.Key).QueueCommands(group);
    }

    /// <summary>
    ///     Schedule a command to run in the future
    /// </summary>
    /// <param name="commandArgs">The argument object corresponding to the desired command</param>
    /// <param name="runTimes">How many times the command should be scheduled, unlimited if null</param>
    /// <param name="nextRun">The next time the command should be queued</param>
    /// <param name="frequency">How frequently the command should be queued, will only be queued once if null</param>
    /// <param name="replace">
    ///     Whether to replace a scheduled command if the Command ID matches an existing scheduled command. Will log an error if duplicate is found
    ///     and replace is false
    /// </param>
    /// <typeparam name="TArgs">The type of the argument objects, must be <see cref="CommandArgs" /> or derived</typeparam>
    public void ScheduleCommand<TArgs>(TArgs commandArgs, int? runTimes, DateTimeOffset nextRun, TimeSpan? frequency, bool replace = false)
        where TArgs : CommandArgs
    {
        var cmdRequest = commandArgs.CommandRequest;
        var scheduledCommand = new ScheduledCommand
        {
            NextRunTime = nextRun.UtcDateTime,
            RunsLeft = runTimes,
            FrequencyMinutes = frequency?.TotalMinutes,
            Priority = cmdRequest.Priority,
            QueueType = cmdRequest.QueueType,
            CommandId = cmdRequest.CommandId,
            CommandArgs = cmdRequest.CommandArgs,
        };
        using var context = _contextFactory.CreateDbContext();
        using var trans = context.Database.BeginTransaction();
        var eScheduledCommand = context.ScheduledCommands.FirstOrDefault(cr => cr.CommandId == scheduledCommand.CommandId);
        if (eScheduledCommand is not null)
        {
            if (replace)
            {
                scheduledCommand.Id = eScheduledCommand.Id;
                var oldRuns = eScheduledCommand.RunsLeft;
                var oldNextRun = eScheduledCommand.NextRunTime.ToLocalTime();
                var oldFreq = eScheduledCommand.FrequencyMinutes;
                context.Entry(eScheduledCommand).CurrentValues.SetValues(scheduledCommand);
                if (context.Entry(eScheduledCommand).State != EntityState.Unchanged)
                    _logger.LogInformation(
                        "Scheduled command for {CommandId} was updated, previous values: {RunTimes} runs, starting at {NextRun}, every {Frequency} minutes",
                        scheduledCommand.CommandId, oldRuns, oldNextRun, oldFreq);
            }
            else
            {
                _logger.LogWarning("Command {CommandId} already scheduled, ignoring", scheduledCommand.CommandId);
                return;
            }
        }
        else
        {
            context.ScheduledCommands.Add(scheduledCommand);
        }

        context.SaveChanges();
        trans.Commit();
        _logger.LogInformation("Command {CommandId} scheduled for {RunTimes} runs, starting at {NextRun}, every {Frequency} minutes",
            scheduledCommand.CommandId, runTimes, nextRun.LocalDateTime, frequency?.TotalMinutes);
    }

    /// <summary>
    ///     Create scheduled commands periodically
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            CreateScheduledCommands();
    }

    /// <summary>
    ///     Queue any scheduled commands that are due
    /// </summary>
    private void CreateScheduledCommands()
    {
        using var context = _contextFactory.CreateDbContext();
        List<ScheduledCommand> scheduledCommands;

        // Suppress the logging of scheduled command DB requests because it clutters the log
        using (SerilogExtensions.SuppressLogging("Microsoft.EntityFrameworkCore.Database.Command"))
        {
            scheduledCommands = context.ScheduledCommands.DueCommands().ToList();
        }

        if (scheduledCommands.Count == 0)
            return;

        _logger.LogInformation("Dispatching {Count} scheduled commands", scheduledCommands.Count);
        var commandArgs = scheduledCommands.Select(sc => sc.CommandArgs).ToList();
        Dispatch(commandArgs);
        foreach (var cmd in scheduledCommands)
            // Remove commands that do not have any runs left or a frequency set
            if (cmd.RunsLeft <= 1 || cmd.FrequencyMinutes is null)
            {
                context.ScheduledCommands.Remove(cmd);
            }
            else
            {
                cmd.RunsLeft -= 1;
                // The next run time is set based on the time that the command was queued plus the frequency, not from the time the command was run
                cmd.NextRunTime = DateTime.UtcNow + TimeSpan.FromMinutes(cmd.FrequencyMinutes.Value);
            }

        context.SaveChanges();
    }
}
