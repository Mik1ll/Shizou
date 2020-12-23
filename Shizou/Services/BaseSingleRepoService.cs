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
    {
        protected readonly ILogger<BaseSingleRepoService<TRepository, TEntity>> _logger;
        protected readonly IRepository<TEntity> _repo;

        public BaseSingleRepoService(ILogger<BaseSingleRepoService<TRepository, TEntity>> logger, IRepository<TEntity> repo)
        {
            _logger = logger;
            _repo = repo;
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }

        public TEntity Get(int id)
        {
            return _repo.Get(id);
        }

        public IEnumerable<TEntity> GetAll()
        {
            return _repo.GetAll();
        }

        public void Save(TEntity entity)
        {
            _repo.Save(entity);
        }
    }
}
