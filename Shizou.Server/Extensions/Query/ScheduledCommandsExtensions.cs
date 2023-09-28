using System;
using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class ScheduledCommandsExtensions
{
    public static IQueryable<ScheduledCommand> DueCommands(this IQueryable<ScheduledCommand> query, ShizouContext context, QueueType queueType)
    {
        return from cq in query
            where cq.QueueType == queueType &&
                  cq.NextRunTime < DateTime.UtcNow &&
                  !context.CommandRequests.Any(cr => cr.CommandId == cq.CommandId)
            select cq;
    }
}
