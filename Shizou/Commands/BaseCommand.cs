using System.Reflection;
using System.Text.Json;
using Shizou.CommandProcessors;
using Shizou.Entities;

namespace Shizou.Commands
{
    public interface ICommand
    {
        bool Completed { get; set; }
        CommandRequest CommandRequest { get; }
        void Process();
        string CommandId { get; }
    }

    public abstract class BaseCommand<T> : ICommand where T : CommandParams
    {
        public bool Completed { get; set; } = false;
        protected T CommandParams { get; }

        public abstract string CommandId { get; }

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
                    CommandId = CommandId,
                    CommandParams = JsonSerializer.Serialize(CommandParams)
                };
            }
        }

        public abstract void Process();
    }
}
