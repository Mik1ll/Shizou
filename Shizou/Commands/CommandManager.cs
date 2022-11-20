using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shizou.Database;
using Shizou.Models;

namespace Shizou.Commands
{
    public class CommandManager
    {
        public static readonly List<(CommandAttribute cmdAttr, Type type, Type paramType, Func<IServiceProvider, CommandParams, ICommand> ctor)> Commands =
            Assembly
            .GetExecutingAssembly().GetTypes()
            .Select(t => new { type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>() })
            .Where(x => x.commandAttr is not null)
            .Select(x =>
            {
                var paramType = x.type.BaseType!.GetGenericArguments()[0];
                Func<IServiceProvider, CommandParams, ICommand> ctor = (provider, cmdParams) =>
                    (ICommand)Activator.CreateInstance(x.type, provider, cmdParams)!;
                return (
                    x.commandAttr!,
                    x.type,
                    paramType,
                    ctor
                );
            })
            .ToList();

        private readonly IServiceProvider _serviceProvider;

        public CommandManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Dispatch<TParams>(TParams commandParams)
            where TParams : CommandParams
        {
            var context = _serviceProvider.GetRequiredService<ShizouContext>();
            var cmdRequest = RequestFromParams(commandParams);
            using var transaction = context.Database.BeginTransaction();
            if (!context.CommandRequests.Any(cr => cr.CommandId == cmdRequest.CommandId))
                context.CommandRequests.Add(cmdRequest);
            context.SaveChanges();
            transaction.Commit();
        }

        public void DispatchRange<TParams>(IEnumerable<TParams> commandParamsEnumerable)
            where TParams : CommandParams
        {
            var context = _serviceProvider.GetRequiredService<ShizouContext>();
            using var transaction = context.Database.BeginTransaction();
            context.CommandRequests.AddRange(
                commandParamsEnumerable.Select(commandParams => RequestFromParams(commandParams))
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
            return command.ctor(_serviceProvider, (CommandParams)JsonSerializer.Deserialize(commandRequest.CommandParams, command.paramType)!);
        }

        public CommandRequest RequestFromParams(CommandParams commandParams)
        {
            var paramType = commandParams.GetType();
            var commandAttr = Commands.Single(x => x.paramType == paramType).cmdAttr;
            return new CommandRequest
            {
                Type = commandAttr.Type,
                Priority = commandAttr.Priority,
                QueueType = commandAttr.QueueType,
                CommandId = commandParams.CommandId,
                CommandParams = JsonSerializer.Serialize(commandParams, paramType)
            };
        }
    }
}
