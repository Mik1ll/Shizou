using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Entities;

namespace Shizou.Commands
{
    public interface ICommand
    {
        bool Completed { get; set; }
        CommandRequest CommandRequest { get; }
        string CommandId { get; }
        Task Process();
    }

    public abstract class BaseCommand<T> : ICommand where T : CommandParams
    {
        protected readonly ILogger<BaseCommand<T>> Logger;

        protected BaseCommand(T commandParams, ILogger<BaseCommand<T>> logger)
        {
            CommandParams = commandParams;
            Logger = logger;
        }

        protected T CommandParams { get; }
        public bool Completed { get; set; }
        public abstract string CommandId { get; }

        public CommandRequest CommandRequest
        {
            get
            {
                var commandAttr = GetType().GetCustomAttribute<CommandAttribute>() ??
                                  throw new InvalidOperationException($"Could not load command attribute from {GetType().Name}");
                return new CommandRequest
                {
                    Type = commandAttr.Type,
                    Priority = commandAttr.Priority,
                    QueueType = commandAttr.QueueType,
                    CommandId = CommandId,
                    CommandParams = JsonSerializer.Serialize(CommandParams)
                };
            }
        }

        public abstract Task Process();
    }
}
