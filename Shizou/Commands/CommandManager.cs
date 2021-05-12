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
        public static readonly Dictionary<CommandType, (Type type, Type paramType)> Commands = Assembly
            .GetExecutingAssembly().GetTypes()
            .Select(t => new {type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>()})
            .Where(x => x.commandAttr is not null)
            .ToDictionary(x => x.commandAttr!.Type, x => (x.type, x.type.BaseType!.GetGenericArguments()[0]));

        public static readonly Dictionary<Type, Type> TypeFromParam = Commands.ToDictionary(x => x.Value.paramType, x => x.Value.type);

        private readonly IServiceProvider _serviceProvider;

        public CommandManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Dispatch<TParams>(TParams commandParams)
            where TParams : CommandParams
        {
            using var context = new ShizouContext();
            context.CommandRequests.Add(((ICommand)ActivatorUtilities.CreateInstance(_serviceProvider, TypeFromParam[commandParams.GetType()], commandParams)).CommandRequest);
            context.SaveChanges();
        }

        public ICommand CommandFromRequest(CommandRequest commandRequest)
        {
            var (type, paramType) = Commands[commandRequest.Type];
            return (ICommand)ActivatorUtilities.CreateInstance(_serviceProvider, type, JsonSerializer.Deserialize(commandRequest.CommandParams, paramType)!);
        }
    }
}
