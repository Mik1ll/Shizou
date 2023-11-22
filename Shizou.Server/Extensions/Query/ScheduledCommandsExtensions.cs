using System;
using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class ScheduledCommandsExtensions
{
    public static IQueryable<ScheduledCommand> DueCommands(this IQueryable<ScheduledCommand> query)
    {
        return from cq in query
            where cq.NextRunTime < DateTime.UtcNow
            select cq;
    }
}
