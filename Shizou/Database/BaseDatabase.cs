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
        protected readonly IConfiguration _configuration;

        private BaseDatabase()
        {
        }

        public BaseDatabase(ILogger<BaseDatabase> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public abstract void CreateDatabase();

        public abstract IDbConnection GetConnection();

        public abstract string GetConnectionString();

        public abstract string GetDatabasePath();
    }
}
