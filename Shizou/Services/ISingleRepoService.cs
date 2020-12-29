using Shizou.Entities;
using System.Collections.Generic;

namespace Shizou.Services
{
    public interface ISingleRepoService<TEntity> where TEntity : Entity, new()
    {
        void Delete(long id);
        TEntity Get(long id);
        IEnumerable<TEntity> GetAll();
        long Save(TEntity entity);
    }
}