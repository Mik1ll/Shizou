using System.Collections.Generic;
using System.Data;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Repositories
{
    public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : Entity, new()
    {
        protected readonly IDatabase _database;
        protected readonly ILogger<BaseRepository<TEntity>> _logger;

        public BaseRepository(ILogger<BaseRepository<TEntity>> logger, IDatabase database)
        {
            _database = database;
            _logger = logger;
        }

        public void Delete(long id)
        {
            IDbConnection cnn = _database.GetConnection();
            if (!cnn.Delete(new TEntity { Id = id }))
                throw new KeyNotFoundException($"Record {typeof(TEntity).Name}:{id} not found in database");
        }

        public TEntity Get(long id)
        {
            IDbConnection cnn = _database.GetConnection();
            return cnn.Get<TEntity>(id);
        }

        public IEnumerable<TEntity> GetAll()
        {
            IDbConnection cnn = _database.GetConnection();
            return cnn.GetAll<TEntity>();
        }

        public void Save(TEntity entity)
        {
            IDbConnection cnn = _database.GetConnection();
            using System.Data.IDbTransaction trans = cnn.BeginTransaction();
            if (entity.Id == 0)
                entity.Id = cnn.Insert(entity, trans);
            else if (!cnn.Update(entity, trans))
                throw new KeyNotFoundException($"Record {typeof(TEntity).Name}:{entity.Id} not found in database");
            trans.Commit();
        }
    }
}