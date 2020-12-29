using Dapper;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Repositories
{
    public interface IImportFolderRepository : IRepository<ImportFolder>
    {
        ImportFolder GetByLocation(string location);
    }

    public class ImportFolderRepository : BaseRepository<ImportFolder>, IImportFolderRepository
    {
        public ImportFolderRepository(ILogger<ImportFolderRepository> logger, IDatabase database) : base(logger, database)
        {
        }

        public ImportFolder GetByLocation(string location)
        {
            var cnn = _database.GetConnection();
            return cnn.QuerySingle<ImportFolder>($"SELECT * FROM ImportFolders WHERE Location = @Location", new { Location = location });
        }
    }
}
