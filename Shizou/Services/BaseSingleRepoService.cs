using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Shizou.Entities;
using Shizou.Repositories;

namespace Shizou.Services
{
    public abstract class BaseSingleRepoService<TRepository, TEntity> : ISingleRepoService<TEntity>
        where TRepository : IRepository<TEntity>
        where TEntity : Entity, new()
    {
        protected readonly ILogger<BaseSingleRepoService<TRepository, TEntity>> Logger;
        protected readonly TRepository Repo;

        protected BaseSingleRepoService(ILogger<BaseSingleRepoService<TRepository, TEntity>> logger, TRepository repo)
        {
            Logger = logger;
            Repo = repo;
        }

        public void Delete(long id)
        {
            Repo.Delete(id);
        }

        public TEntity Get(long id)
        {
            return Repo.Get(id);
        }

        public IEnumerable<TEntity> GetAll()
        {
            return Repo.GetAll();
        }

        public void Save(TEntity entity)
        {
            Repo.Save(entity);
        }
    }
}
