using System;
using Shizou.Data.Enums;

namespace Shizou.Server.Commands;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandAttribute : Attribute
{
    public CommandAttribute(Type commandType, CommandPriority priority, QueueType queueType)
    {
        CommandType = commandType;
        Priority = priority;
        QueueType = queueType;
    }

    public Type CommandType { get; }
    public CommandPriority Priority { get; }
    public QueueType QueueType { get; }
}
