using Microsoft.Extensions.Logging;
using Shizou.Entities;
using Shizou.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Services
{
    public abstract class BaseSingleRepoService<TRepository, TEntity> : ISingleRepoService<TEntity>
        where TRepository : IRepository<TEntity>
        where TEntity : Entity, new()
    {
        protected readonly ILogger<BaseSingleRepoService<TRepository, TEntity>> _logger;
        protected readonly TRepository _repo;

        public BaseSingleRepoService(ILogger<BaseSingleRepoService<TRepository, TEntity>> logger, TRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        public void Delete(long id)
        {
            _repo.Delete(id);
        }

        public TEntity Get(long id)
        {
            return _repo.Get(id);
        }

        public IEnumerable<TEntity> GetAll()
        {
            return _repo.GetAll();
        }

        public long Save(TEntity entity)
        {
            return _repo.Save(entity);
        }
    }
}
