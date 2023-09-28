using System.Linq;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class CommandRequestsExtensions
{
    public static IQueryable<CommandRequest> ByQueueOrdered(this IQueryable<CommandRequest> query, QueueType queueType)
    {
        return from cq in query
            where cq.QueueType == queueType
            orderby cq.Priority, cq.Id
            select cq;
    }

    public static CommandRequest? Next(this IQueryable<CommandRequest> query, QueueType queueType)
    {
        return query.ByQueueOrdered(queueType).FirstOrDefault();
    }
}
