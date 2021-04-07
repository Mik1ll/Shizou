using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Commands;
using Shizou.Entities;

namespace Shizou.Extensions
{
    public static class CommandRequestExtensions
    {
        public static BaseCommand? GetNextCommand(this DbSet<CommandRequest> commandRequests, QueueType queueType)
        {
            return (from cq in commandRequests
                where cq.QueueType == queueType
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
