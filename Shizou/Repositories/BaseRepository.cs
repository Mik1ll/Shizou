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
            var cnn = _database.GetConnection();
            cnn.Delete(new TEntity { Id = id });
        }

        public TEntity Get(long id)
        {
            var cnn = _database.GetConnection();
            return cnn.Get<TEntity>(id);
        }

        public IEnumerable<TEntity> GetAll()
        {
            var cnn = _database.GetConnection();
            return cnn.GetAll<TEntity>();
        }

        public long Save(TEntity entity)
        {
            var cnn = _database.GetConnection();
            using var trans = cnn.BeginTransaction();
            if (entity.Id == 0 || !cnn.Update(entity, trans))
                entity.Id = cnn.Insert(entity, trans);
            trans.Commit();
            return entity.Id;
        }
    }
}
