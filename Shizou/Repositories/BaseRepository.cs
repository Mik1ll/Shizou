using Microsoft.Extensions.Logging;
using Shizou.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace Shizou.Repositories
{
    public class BaseRepository<TModel> : IRepository<TModel> where TModel : class
    {
        protected readonly IDatabase _database;
        protected readonly ILogger<BaseRepository<TModel>> _logger;

        private BaseRepository()
        {
        }

        public BaseRepository(ILogger<BaseRepository<TModel>> logger, IDatabase database)
        {
            _database = database;
            _logger = logger;
        }

        public void Delete(int id)
        {
            using (var cnn = _database.GetConnection())
            {
            }
        }

        public TModel Get(int id)
        {
            throw new NotImplementedException();
        }

        public List<TModel> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Save(TModel model)
        {
            throw new NotImplementedException();
        }
    }
}
