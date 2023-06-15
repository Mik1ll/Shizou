using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Commands;

namespace Shizou.Server.Services;

public class CommandService
{
    public static readonly List<(CommandAttribute cmdAttr, Type type, Type argType, Func<IServiceProvider, CommandArgs, ICommand> ctor)> Commands =
        Assembly
            .GetExecutingAssembly().GetTypes()
            .Select(t => new { type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>() })
            .Where(x => x.commandAttr is not null)
            .Select(x =>
            {
                var argType = x.type.BaseType!.GetGenericArguments()[0];
                Func<IServiceProvider, CommandArgs, ICommand> ctor = (provider, cmdArgs) =>
                    (ICommand)Activator.CreateInstance(x.type, provider, cmdArgs)!;
                return (
                    x.commandAttr!,
                    x.type,
                    argType,
                    ctor
                );
            })
            .ToList();

    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly List<CommandProcessor> _processors;

    public CommandService(IServiceProvider serviceProvider, IDbContextFactory<ShizouContext> contextFactory, IEnumerable<CommandProcessor> processors)
    {
        _serviceProvider = serviceProvider;
        _contextFactory = contextFactory;
        _processors = processors.ToList();
    }

    public void Dispatch<TArgs>(TArgs commandArgs)
        where TArgs : CommandArgs
    {
        using var context = _contextFactory.CreateDbContext();
        var cmdRequest = RequestFromArgs(commandArgs);
        var processor = _processors.Single(cp => cp.QueueType == cmdRequest.QueueType);
        using var transaction = context.Database.BeginTransaction();
        if (!context.CommandRequests.Any(cr => cr.CommandId == cmdRequest.CommandId))
            context.CommandRequests.Add(cmdRequest);
        context.SaveChanges();
        transaction.Commit();
        processor.UpdateCommandsInQueue();
    }

    public void DispatchRange<TArgs>(IEnumerable<TArgs> commandArgsEnumerable)
        where TArgs : CommandArgs
    {
        using var context = _contextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        var commandRequests = commandArgsEnumerable.Select(commandArgs => RequestFromArgs(commandArgs))
            // Throw away identical command ids
            .GroupBy(cr => cr.CommandId)
            .Select(crs => crs.First())
            // Left outer join, exclude commands already in database
            .Where(e => !context.CommandRequests.Any(c => c.CommandId == e.CommandId))
            .ToList();
        context.CommandRequests.AddRange(commandRequests);
        context.SaveChanges();
        transaction.Commit();
        foreach (var processor in _processors.Where(p => commandRequests.Select(cr => cr.QueueType).Distinct().Any(qt => qt == p.QueueType)))
            processor.UpdateCommandsInQueue();
    }

    public ICommand CommandFromRequest(CommandRequest commandRequest)
    {
        var command = Commands.Single(x => commandRequest.Type == x.cmdAttr.Type);
        return command.ctor(_serviceProvider, (CommandArgs)JsonSerializer.Deserialize(commandRequest.CommandArgs, command.argType)!);
    }

    public CommandRequest RequestFromArgs(CommandArgs commandArgs)
    {
        var argType = commandArgs.GetType();
        var commandAttr = Commands.Single(x => x.argType == argType).cmdAttr;
        return new CommandRequest
        {
            Type = commandAttr.Type,
            Priority = commandAttr.Priority,
            QueueType = commandAttr.QueueType,
            CommandId = commandArgs.CommandId,
            CommandArgs = JsonSerializer.Serialize(commandArgs, argType)
        };
    }
}
