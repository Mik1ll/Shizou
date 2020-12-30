using System.Collections.Generic;
using Shizou.Entities;

namespace Shizou.Services
{
    public interface ISingleRepoService<TEntity> where TEntity : Entity, new()
    {
        void Delete(long id);

        TEntity Get(long id);

        IEnumerable<TEntity> GetAll();

        void Save(TEntity entity);
    }
}