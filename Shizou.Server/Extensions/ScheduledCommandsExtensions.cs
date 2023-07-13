using System.Linq;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions;

public static class ScheduledCommandsExtensions
{
    public static IQueryable<ScheduledCommand> ByQueue(this IQueryable<ScheduledCommand> queryable, QueueType queueType)
    {
        return from cq in queryable
            where cq.QueueType == queueType
            orderby cq.Priority, cq.Id
            select cq;
    }
}
