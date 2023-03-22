using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Models;

namespace Shizou.Services;

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

    public CommandService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Dispatch<TArgs>(TArgs commandArgs)
        where TArgs : CommandArgs
    {
        var context = _serviceProvider.GetRequiredService<ShizouContext>();
        var cmdRequest = RequestFromArgs(commandArgs);
        using var transaction = context.Database.BeginTransaction();
        if (!context.CommandRequests.Any(cr => cr.CommandId == cmdRequest.CommandId))
            context.CommandRequests.Add(cmdRequest);
        context.SaveChanges();
        transaction.Commit();
    }

    public void DispatchRange<TArgs>(IEnumerable<TArgs> commandArgsEnumerable)
        where TArgs : CommandArgs
    {
        var context = _serviceProvider.GetRequiredService<ShizouContext>();
        using var transaction = context.Database.BeginTransaction();
        context.CommandRequests.AddRange(
            commandArgsEnumerable.Select(commandArgs => RequestFromArgs(commandArgs))
                // Throw away identical command ids
                .GroupBy(cr => cr.CommandId)
                .Select(crs => crs.First())
                // Left outer join, exclude commands already in database
                .Where(e => !context.CommandRequests.Any(c => c.CommandId == e.CommandId))
        );
        context.SaveChanges();
        transaction.Commit();
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