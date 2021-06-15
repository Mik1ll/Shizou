using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Commands
{
    public class CommandManager
    {
        public static readonly List<(CommandType cmdType, Type type, Type paramType, ConstructorInfo ctor)> Commands = Assembly
            .GetExecutingAssembly().GetTypes()
            .Select(t => new {type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>()})
            .Where(x => x.commandAttr is not null)
            .Select(x => (
                x.commandAttr!.Type,
                x.type,
                x.type.BaseType!.GetGenericArguments()[0],
                x.type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis,
                    new[] {typeof(IServiceProvider), typeof(CommandParams)}, null)!))
            .ToList();

        private readonly IServiceProvider _serviceProvider;

        public CommandManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // TODO: Test new implementation without activatorutils
        public void Dispatch<TParams>(TParams commandParams)
            where TParams : CommandParams
        {
            using var context = new ShizouContext();
            var command = Commands.First(x => commandParams.GetType() == x.paramType);
            context.CommandRequests.Add(((ICommand)command.ctor.Invoke(new object[] {_serviceProvider, commandParams})).CommandRequest);
            context.SaveChanges();
        }

        public ICommand CommandFromRequest(CommandRequest commandRequest)
        {
            var command = Commands.First(x => commandRequest.Type == x.cmdType);
            return (ICommand)command.ctor.Invoke(new[] {_serviceProvider, JsonSerializer.Deserialize(commandRequest.CommandParams, command.paramType)!});
        }
    }
}
