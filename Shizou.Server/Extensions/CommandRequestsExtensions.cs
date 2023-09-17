using System.Linq;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions;

public static class CommandRequestsExtensions
{
    public static IQueryable<CommandRequest> ByQueue(this IQueryable<CommandRequest> queryable, QueueType queueType)
    {
        return from cq in queryable
            where cq.QueueType == queueType
            orderby cq.Priority, cq.Id
            select cq;
    }

    public static CommandRequest? NextRequest(this IQueryable<CommandRequest> commandRequests, QueueType queueType)
    {
        return commandRequests.ByQueue(queueType).FirstOrDefault();
    }
}
