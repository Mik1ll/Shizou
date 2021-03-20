using System;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Repositories
{
    interface ICommandRequestRepository : IRepository<CommandRequest>
    {
    }
    
    public class CommandRequestRepository : BaseRepository<CommandRequest>,ICommandRequestRepository
    {
        public CommandRequestRepository(ILogger<BaseRepository<CommandRequest>> logger, IDatabase database) : base(logger, database)
        {
        }

        public CommandRequest GetNext()
        {
            IDbConnection cnn = Database.Connection;
            return cnn.QuerySingle<CommandRequest>("SELECT * FROM CommandRequests ORDER BY Priority, Id LIMIT 1");
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
    }
}
