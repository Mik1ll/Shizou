using System.Collections.Generic;

namespace Shizou.Services
{
    public interface ISingleRepoService<TEntity>
    {
        void Delete(int id);
        TEntity Get(int id);
        IEnumerable<TEntity> GetAll();
        void Save(TEntity entity);
    }
}