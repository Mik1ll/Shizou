using Dapper;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Entities;

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
            System.Data.IDbConnection? cnn = _database.GetConnection();
            return cnn.QuerySingle<ImportFolder>($"SELECT * FROM ImportFolders WHERE Location = @Location", new { Location = location });
        }
    }
}