using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Commands
{
    public class CommandManager
    {
        public static readonly List<(CommandType cmdType, Type type, Type paramType, Func<IServiceProvider, CommandParams, ICommand> ctor)> Commands = Assembly
            .GetExecutingAssembly().GetTypes()
            .Select(t => new {type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>()})
            .Where(x => x.commandAttr is not null)
            .Select(x =>
            {
                var paramType = x.type.BaseType!.GetGenericArguments()[0];
                Func<IServiceProvider, CommandParams, ICommand> ctor = (provider, cmdParams) =>
                    (ICommand)x.type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis,
                        new[] {typeof(IServiceProvider), paramType}, null)!.Invoke(new object[] {provider, cmdParams});
                return (
                    x.commandAttr!.Type,
                    x.type,
                    paramType
                    , ctor
                );
            })
            .ToList();

        private readonly IServiceProvider _serviceProvider;

        public CommandManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // TODO: DispatchRange
        // TODO: Test new implementation without activatorutils
        public void Dispatch<TParams>(TParams commandParams)
            where TParams : CommandParams
        {
            var context = _serviceProvider.GetRequiredService<ShizouContext>();
            var command = Commands.First(x => commandParams.GetType() == x.paramType);
            var cmdRequest = command.ctor(_serviceProvider, commandParams).CommandRequest;
            if (!context.CommandRequests.Any(cr => cr.CommandId == cmdRequest.CommandId))
                context.CommandRequests.Add(cmdRequest);
            context.SaveChanges();
        }

        public void DispatchRange<TParams>(IEnumerable<TParams> commandParamsEnumerable)
            where TParams : CommandParams
        {
            var context = _serviceProvider.GetRequiredService<ShizouContext>();
            context.CommandRequests.AddRange(
                commandParamsEnumerable.Select(commandParams =>
                        Commands.First(x => commandParams.GetType() == x.paramType)
                            .ctor(_serviceProvider, commandParams).CommandRequest)
                    // Throw away identical command ids
                    .GroupBy(cr => cr.CommandId)
                    .Select(crs => crs.First())
                    // Left outer join, exclude commands already in database
                    .GroupJoin(
                        context.CommandRequests,
                        e => e.CommandId,
                        e => e.CommandId,
                        (request, requests) => new {Request = request, DbRequest = requests.FirstOrDefault()})
                    .Where(e => e.DbRequest is null)
                    .Select(e => e.Request)
            );
            context.SaveChanges();
        }

        public ICommand CommandFromRequest(CommandRequest commandRequest)
        {
            var command = Commands.First(x => commandRequest.Type == x.cmdType);
            return command.ctor(_serviceProvider, (CommandParams)JsonSerializer.Deserialize(commandRequest.CommandParams, command.paramType)!);
        }
    }
}
