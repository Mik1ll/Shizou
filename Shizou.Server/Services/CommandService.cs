using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Commands;
using Shizou.Server.Extensions.Query;

namespace Shizou.Server.Services;

public class CommandService
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
        var cmdRequest = commandArgs.CommandRequest;
        var processor = _processors.Single(cp => cp.QueueType == cmdRequest.QueueType);
        processor.QueueCommand(cmdRequest);
    }

    public void DispatchRange<TArgs>(IEnumerable<TArgs> commandArgsEnumerable)
        where TArgs : CommandArgs
    {
        foreach (var cmdArgs in commandArgsEnumerable)
            Dispatch(cmdArgs);
    }

    public void ScheduleCommand<TArgs>(TArgs commandArgs, int? runTimes, DateTimeOffset nextRun, TimeSpan? frequency)
        where TArgs : CommandArgs
    {
        using var context = _contextFactory.CreateDbContext();
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
        if (!context.ScheduledCommands.Any(cr => cr.CommandId == scheduledCommand.CommandId))
        {
            context.ScheduledCommands.Add(scheduledCommand);
            context.SaveChanges();
            _logger.LogInformation("Command {CommandId} scheduled for {RunTimes} runs, starting at {NextRun}, every {Frequency} minutes",
                scheduledCommand.CommandId, runTimes, nextRun.LocalDateTime, frequency?.TotalMinutes);
        }
        else
        {
            _logger.LogWarning("Command {CommandId} already scheduled, ignoring", scheduledCommand.CommandId);
        }
    }

    public void CreateScheduledCommands(QueueType queueType)
    {
        using var context = _contextFactory.CreateDbContext();
        var scheduledCommands = context.ScheduledCommands.DueCommands(context, queueType).ToList();
        if (scheduledCommands.Count == 0)
            return;
        var commandArgs = scheduledCommands.Select(scheduledCommand =>
        {
            var args = JsonSerializer.Deserialize(scheduledCommand.CommandArgs, CommandArgs.GetJsonTypeInfo())!;
            return args;
        }).ToList();
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
