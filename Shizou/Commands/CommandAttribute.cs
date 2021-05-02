using System;
using Shizou.Enums;

namespace Shizou.Commands
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CommandAttribute : Attribute
    {
        public CommandAttribute(CommandType type, CommandPriority priority, QueueType queueType, Type paramType)
        {
            Type = type;
            Priority = priority;
            QueueType = queueType;
            ParamType = paramType;
        }

        public CommandType Type { get; }
        public CommandPriority Priority { get; }
        public QueueType QueueType { get; }
        public Type ParamType { get; }
    }
}
