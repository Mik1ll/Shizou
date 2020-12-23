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
using Shizou.Entities;

namespace Shizou.Repositories
{
    public interface IRepository<TEntity>
    {
        IEnumerable<TEntity> GetAll();
        TEntity Get(int id);
        void Save(TEntity entity);
        void Delete(int id);
    }

    public class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity, new()
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
            var cnn = _database.GetConnection();
            cnn.Delete(new TEntity { Id = id });
        }

        public TEntity Get(int id)
        {
            var cnn = _database.GetConnection();
            return cnn.Get<TEntity>(id);
        }

        public IEnumerable<TEntity> GetAll()
        {
            var cnn = _database.GetConnection();
            return cnn.GetAll<TEntity>();
        }

        public void Save(TEntity entity)
        {
            var cnn = _database.GetConnection();
            using var trans = cnn.BeginTransaction();
            var updated = cnn.Update(entity, trans);
            if (!updated)
                cnn.Insert(entity, trans);
        }
    }
}
