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
        public static readonly Dictionary<CommandType, (Type paramType, Func<IServiceProvider, CommandParams, ICommand> create)> Commands = Assembly
            .GetExecutingAssembly().GetTypes()
            .Select(t => new {type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>()})
            .Where(x => x.commandAttr is not null)
            .ToDictionary(
                x => x.commandAttr!.Type,
                x => (x.type.BaseType!.GetGenericArguments()[0],
                    new Func<IServiceProvider, CommandParams, ICommand>((provider, commandParams) =>
                        (ICommand)ActivatorUtilities.CreateInstance(provider, x.type, commandParams)!)));

        private readonly IServiceProvider _serviceProvider;
        private readonly ShizouContext _context;

        public CommandManager(IServiceProvider serviceProvider, ShizouContext context)
        {
            _serviceProvider = serviceProvider;
            _context = context;
        }

        public void Dispatch<TCommand, TParams>(TParams commandParams)
            where TCommand : BaseCommand<TParams>
            where TParams : CommandParams
        {
            _context.CommandRequests.Add(ActivatorUtilities.CreateInstance<TCommand>(_serviceProvider, commandParams).CommandRequest);
            _context.SaveChanges();
        }

        public ICommand CommandFromRequest(CommandRequest commandRequest)
        {
            var (paramType, create) = Commands[commandRequest.Type];
            return create(_serviceProvider, (CommandParams)JsonSerializer.Deserialize(commandRequest.CommandParams, paramType)!);
        }
    }
}
