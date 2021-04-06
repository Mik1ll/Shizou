using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Logging;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Repositories
{
    public interface ICommandRequestRepository : IRepository<CommandRequest>
    {
        BaseCommand GetNextCommand(QueueType queueType);
        int GetQueueCount(QueueType queueType);
        void ClearQueue(QueueType queueType);
    }

    public class CommandRequestRepository : BaseRepository<CommandRequest>, ICommandRequestRepository
    {
        public CommandRequestRepository(ILogger<BaseRepository<CommandRequest>> logger, IDatabase database) : base(logger, database)
        {
        }

        public override void Save(CommandRequest entity)
        {
            try
            {
                base.Save(entity);
            }
            catch (ConstraintException)
            {
            }
        }

        public BaseCommand GetNextCommand(QueueType queueType)
        {
            IDbConnection cnn = Database.Connection;
            return cnn.QuerySingle<CommandRequest>("SELECT * FROM CommandRequests WHERE QueueType = @QueueType ORDER BY Priority, Id LIMIT 1",
                new {QueueType = queueType}).Command;
        }

        public int GetQueueCount(QueueType queueType)
        {
            return Database.Connection.QuerySingle<int>("SELECT COUNT(*) FROM CommandRequests WHERE QueueType = @QueueType", new {QueueType = queueType});
        }

        public void ClearQueue(QueueType queueType)
        {
            Database.Connection.Execute("DELETE FROM CommandRequests WHERE QueueType = @QueueType", new {QueueType = queueType});
        }
    }
}
