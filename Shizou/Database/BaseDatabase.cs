using Microsoft.Extensions.Logging;
using System.Data;

namespace Shizou.Database
{
    public abstract class BaseDatabase : IDatabase
    {
        protected readonly ILogger<BaseDatabase> _logger;

        public BaseDatabase(ILogger<BaseDatabase> logger)
        {
            _logger = logger;
        }

        public abstract IDbConnection GetConnection();
        public abstract string GetConnectionString();
        public abstract bool DatabaseExists();
        public abstract void CreateDatabase();
        public abstract void BackupDatabase();
        public abstract void CreateSchema();
    }
}
