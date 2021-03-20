using System.Collections.Generic;
using System.Data;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou.Repositories
{
    public abstract class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : Entity, new()
    {
        protected readonly IDatabase Database;
        protected readonly ILogger<BaseRepository<TEntity>> Logger;

        protected BaseRepository(ILogger<BaseRepository<TEntity>> logger, IDatabase database)
        {
            Database = database;
            Logger = logger;
        }

        public virtual void Delete(long id)
        {
            IDbConnection cnn = Database.Connection;
            if (!cnn.Delete(new TEntity {Id = id}))
                throw new KeyNotFoundException($"Record {typeof(TEntity).Name}:{id} not found in database");
        }

        public virtual TEntity Get(long id)
        {
            IDbConnection cnn = Database.Connection;
            return cnn.Get<TEntity>(id);
        }

        public virtual IEnumerable<TEntity> GetAll()
        {
            IDbConnection cnn = Database.Connection;
            return cnn.GetAll<TEntity>();
        }

        public virtual void Save(TEntity entity)
        {
            IDbConnection cnn = Database.Connection;
            using IDbTransaction trans = cnn.BeginTransaction();
            if (entity.Id == 0)
                entity.Id = cnn.Insert(entity, trans);
            else if (!cnn.Update(entity, trans))
                throw new KeyNotFoundException($"Record {typeof(TEntity).Name}:{entity.Id} not found in database");
            trans.Commit();
        }
    }
}
