using Microsoft.Extensions.Logging;
using Shizou.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace Shizou.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly IDatabase _database;
        protected readonly ILogger<Repository<TEntity>> _logger;

        public Repository(ILogger<Repository<TEntity>> logger, IDatabase database)
        {
            _database = database;
            _logger = logger;
        }

        public void Delete(int id)
        {
            using var cnn = _database.GetConnection();
        }

        public TEntity GetByID(int id)
        {
            throw new NotImplementedException();
        }

        public List<TEntity> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Save(TEntity model)
        {
            throw new NotImplementedException();
        }
    }
}
