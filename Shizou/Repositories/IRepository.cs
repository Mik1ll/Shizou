using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Repositories
{
    interface IRepository<TModel>
    {
        List<TModel> GetAll();
        TModel Get(int id);
        void Save(TModel model);
        void Delete(int id);
    }
}
