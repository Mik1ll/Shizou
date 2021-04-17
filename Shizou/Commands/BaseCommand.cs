using System.Reflection;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Commands
{
    public abstract class BaseCommand
    {
        protected readonly CommandPriority Priority;

        protected readonly QueueType QueueType;

        protected readonly CommandType Type;

        public bool Completed = false;

        protected BaseCommand()
        {
            var commandAttr = GetType().GetCustomAttribute<CommandAttribute>();
            Type = commandAttr?.Type ?? CommandType.Invalid;
            Priority = commandAttr?.Priority ?? CommandPriority.Invalid;
            QueueType = commandAttr?.QueueType ?? QueueType.Invalid;
        }

        public CommandRequest CommandRequest =>
            new()
            {
                Type = Type,
                Priority = Priority,
                QueueType = QueueType,
                CommandId = GenerateCommandId(),
                CommandParams = GenerateCommandParams()
            };

        public abstract void Process();

        protected abstract string GenerateCommandId();

        protected abstract string GenerateCommandParams();

        public BaseCommand Init(CommandRequest commandRequest)
        {
            PopulateCommandParams(commandRequest.CommandParams);
            return this;
        }

        protected abstract void PopulateCommandParams(string commandParams);
    }
}
