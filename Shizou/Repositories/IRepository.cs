using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Repositories
{
    interface IRepository<TEntity>
    {
        List<TEntity> GetAll();
        TEntity GetByID(int id);
        void Save(TEntity model);
        void Delete(int id);
    }
}
