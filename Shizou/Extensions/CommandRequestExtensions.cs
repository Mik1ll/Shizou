﻿using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Commands;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Extensions
{
    public static class CommandRequestExtensions
    {
        public static BaseCommand? GetNextCommand(this DbSet<CommandRequest> commandRequests, QueueType queueType, bool allowUdp, bool allowHttp)
        {
            var excludeFlags = (!allowHttp ? CommandType.AniDbHttp : 0) | (!allowUdp ? CommandType.AniDbUdp : 0);
            return (from cq in commandRequests
                where cq.QueueType == queueType && (cq.Type & excludeFlags) == 0
                orderby cq.Priority, cq.Id
                select cq).FirstOrDefault()?.Command;
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
}
