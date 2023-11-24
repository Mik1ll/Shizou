using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Data.Utilities;
using Shizou.Server.CommandProcessors;
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

    public void Dispatch<TArgs>(TArgs commandArgs)
        where TArgs : CommandArgs
    {
        var processor = _processors.Single(cp => cp.QueueType == commandArgs.QueueType);
        processor.QueueCommand(commandArgs.CommandRequest);
    }

    public void DispatchRange<TArgs>(IEnumerable<TArgs> commandArgs)
        where TArgs : CommandArgs
    {
        var grouped = commandArgs.Select(a => a.CommandRequest).GroupBy(a => a.QueueType);
        foreach (var group in grouped)
            _processors.Single(p => p.QueueType == group.Key).QueueCommands(group);
    }

    public void ScheduleCommand<TArgs>(TArgs commandArgs, int? runTimes, DateTimeOffset nextRun, TimeSpan? frequency)
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
            CommandArgs = cmdRequest.CommandArgs
        };
        using var context = _contextFactory.CreateDbContext();
        using var trans = context.Database.BeginTransaction();
        if (context.ScheduledCommands.Any(cr => cr.CommandId == scheduledCommand.CommandId))
        {
            _logger.LogWarning("Command {CommandId} already scheduled, ignoring", scheduledCommand.CommandId);
            return;
        }

        context.ScheduledCommands.Add(scheduledCommand);
        context.SaveChanges();
        trans.Commit();
        _logger.LogInformation("Command {CommandId} scheduled for {RunTimes} runs, starting at {NextRun}, every {Frequency} minutes",
            scheduledCommand.CommandId, runTimes, nextRun.LocalDateTime, frequency?.TotalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            CreateScheduledCommands();
    }

    private void CreateScheduledCommands()
    {
        using var context = _contextFactory.CreateDbContext();
        var scheduledCommands = context.ScheduledCommands.DueCommands().ToList();
        if (scheduledCommands.Count == 0)
            return;
        _logger.LogInformation("Dispatching {Count} scheduled commands", scheduledCommands.Count);
        var commandArgs = scheduledCommands.Select(sc =>
            JsonSerializer.Deserialize(sc.CommandArgs, PolymorphicJsonTypeInfo<CommandArgs>.CreateJsonTypeInfo())!).ToList();
        DispatchRange(commandArgs);
        foreach (var cmd in scheduledCommands)
            if (cmd.RunsLeft <= 1 || cmd.FrequencyMinutes is null)
            {
                context.ScheduledCommands.Remove(cmd);
            }
            else
            {
                cmd.RunsLeft -= 1;
                cmd.NextRunTime = DateTime.UtcNow + TimeSpan.FromMinutes(cmd.FrequencyMinutes.Value);
            }

        context.SaveChanges();
    }
}
