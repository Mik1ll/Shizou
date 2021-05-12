using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Entities;

namespace Shizou.Commands
{
    public interface ICommand
    {
        bool Completed { get; set; }
        CommandRequest CommandRequest { get; }
        string CommandId { get; }
        void Process();
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
        public bool Completed { get; set; } = false;
        public abstract string CommandId { get; }

        public CommandRequest CommandRequest
        {
            get
            {
                var commandAttr = GetType().GetCustomAttribute<CommandAttribute>();
                return new CommandRequest
                {
                    Type = commandAttr?.Type ?? CommandType.Invalid,
                    Priority = commandAttr?.Priority ?? CommandPriority.Invalid,
                    QueueType = commandAttr?.QueueType ?? QueueType.Invalid,
                    CommandId = CommandId,
                    CommandParams = JsonSerializer.Serialize(CommandParams)
                };
            }
        }

        public abstract void Process();
    }
}
