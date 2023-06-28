﻿using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Common.Enums;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions;

public static class CommandRequestExtensions
{
    public static CommandRequest? GetNextRequest(this DbSet<CommandRequest> commandRequests, QueueType queueType)
    {
        return (from cq in commandRequests
            where cq.QueueType == queueType
            orderby cq.Priority, cq.Id
            select cq).FirstOrDefault();
    }

    public static int GetQueueCount(this DbSet<CommandRequest> commandRequests, QueueType queueType)
    {
        return commandRequests.Count(cq => cq.QueueType == queueType);
    }

    public static void ClearQueue(this DbSet<CommandRequest> commandRequests, QueueType queueType)
    {
        commandRequests.RemoveRange(commandRequests.Where(cq => cq.QueueType == queueType));
    }
}