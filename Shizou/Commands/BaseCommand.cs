using System.Reflection;
using System.Text.Json;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Commands
{
    public interface ICommand
    {
        bool Completed { get; set; }
        CommandRequest CommandRequest { get; }
        void Process();
    }

    public abstract class BaseCommand<T> : ICommand where T : CommandParams
    {
        public bool Completed { get; set; } = false;
        protected T CommandParams { get; }

        protected BaseCommand(T commandParams)
        {
            CommandParams = commandParams;
        }

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
                    CommandId = GenerateCommandId(),
                    CommandParams = JsonSerializer.Serialize(CommandParams)
                };
            }
        }

        public abstract void Process();

        protected abstract string GenerateCommandId();
    }
}
