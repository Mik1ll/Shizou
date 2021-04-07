using Shizou.Entities;

namespace Shizou.Commands
{
    public abstract class BaseCommand
    {
        protected readonly CommandPriority Priority;

        protected readonly QueueType QueueType;

        protected readonly CommandType Type;

        public bool Completed = false;

        protected BaseCommand(CommandType type, CommandPriority priority, QueueType queueType)
        {
            Type = type;
            Priority = priority;
            QueueType = queueType;
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

        public BaseCommand Init()
        {
            ParamsFromCommandRequest();
            return this;
        }

        protected abstract void ParamsFromCommandRequest();
    }
}
