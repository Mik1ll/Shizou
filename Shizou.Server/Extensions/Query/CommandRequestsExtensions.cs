using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class CommandRequestsExtensions
{
    public static IQueryable<CommandRequest> CommandRequestsByQueueOrdered(this ShizouContext context, QueueType queueType)
    {
        return from cq in context.CommandRequests
            where cq.QueueType == queueType
            orderby cq.Priority, cq.Id
            select cq;
    }

    public static CommandRequest? CommandRequestNext(this ShizouContext context, QueueType queueType)
    {
        return context.CommandRequestsByQueueOrdered(queueType).FirstOrDefault();
    }
}
