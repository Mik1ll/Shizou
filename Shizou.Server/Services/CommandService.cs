using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Commands;

namespace Shizou.Server.Services;

public class CommandService
{
    public static readonly IList<(CommandAttribute cmdAttr, Type type, Type argsType)> Commands =
        (from type in Assembly.GetExecutingAssembly().GetTypes()
            let cmdAttr = type.GetCustomAttribute<CommandAttribute>()
            where cmdAttr is not null
            let argsType = type.BaseType!.GetGenericArguments()[0]
            select (cmdAttr, type, argsType)).ToList();

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
        var args = (CommandArgs)JsonSerializer.Deserialize(commandRequest.CommandArgs, command.argsType)!;
        return (ICommand)ActivatorUtilities.CreateInstance(_serviceProvider, command.type, args);
    }

    public CommandRequest RequestFromArgs(CommandArgs commandArgs)
    {
        var argType = commandArgs.GetType();
        var commandAttr = Commands.Single(x => x.argsType == argType).cmdAttr;
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
