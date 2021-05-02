using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shizou.Database;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Commands
{
    public class CommandManager
    {
        public static readonly Dictionary<CommandType, (Type paramType, Func<IServiceProvider, CommandParams, BaseCommand> create)> Commands = Assembly
            .GetExecutingAssembly().GetTypes()
            .Select(t => new {type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>()})
            .Where(x => x.commandAttr is not null && x.type.IsSubclassOf(typeof(BaseCommand)))
            .ToDictionary(
                x => x.commandAttr!.Type,
                x => (x.commandAttr!.ParamType,
                    new Func<IServiceProvider, CommandParams, BaseCommand>((provider, commandParams) =>
                        (BaseCommand)ActivatorUtilities.CreateInstance(provider, x.type, commandParams)!)));

        private readonly IServiceProvider _serviceProvider;
        private readonly ShizouContext _context;

        public CommandManager(IServiceProvider serviceProvider, ShizouContext context)
        {
            _serviceProvider = serviceProvider;
            _context = context;
        }

        public void Dispatch<T>(CommandParams commandParams) where T : BaseCommand
        {
            _context.CommandRequests.Add(ActivatorUtilities.CreateInstance<T>(_serviceProvider, commandParams).CommandRequest);
            _context.SaveChanges();
        }

        public BaseCommand CommandFromRequest(CommandRequest commandRequest)
        {
            var (paramType, create) = Commands[commandRequest.Type];
            return create(_serviceProvider, (CommandParams)JsonSerializer.Deserialize(commandRequest.CommandParams, paramType)!);
        }
    }
}
