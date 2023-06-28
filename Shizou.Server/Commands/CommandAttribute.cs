using System;
using Shizou.Data.Enums;

namespace Shizou.Server.Commands;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandAttribute : Attribute
{
    public CommandAttribute(CommandType type, CommandPriority priority, QueueType queueType)
    {
        Type = type;
        Priority = priority;
        QueueType = queueType;
    }

    public CommandType Type { get; }
    public CommandPriority Priority { get; }
    public QueueType QueueType { get; }
}