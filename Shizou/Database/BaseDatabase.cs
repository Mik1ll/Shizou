using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Database
{
    public abstract class BaseDatabase : IDatabase
    {
        protected readonly ILogger<BaseDatabase> _logger;

        private BaseDatabase()
        {
        }

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
