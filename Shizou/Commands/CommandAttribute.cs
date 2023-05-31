﻿using System;
using ShizouCommon.Enums;

namespace Shizou.Commands;

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