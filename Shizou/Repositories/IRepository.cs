using System.Collections.Generic;
using Shizou.Entities;

namespace Shizou.Repositories
{
    public interface IRepository<TEntity> where TEntity : Entity, new()
    {
        IEnumerable<TEntity> GetAll();
        TEntity Get(long id);
        long Save(TEntity entity);
        void Delete(long id);
    }
}
