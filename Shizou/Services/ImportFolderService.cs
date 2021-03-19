using Microsoft.Extensions.Logging;
using Shizou.Entities;
using Shizou.Repositories;

namespace Shizou.Services
{
    public interface IImportFolderService : ISingleRepoService<ImportFolder>
    {
        ImportFolder GetByLocation(string location);
    }

    public class ImportFolderService : BaseSingleRepoService<IImportFolderRepository, ImportFolder>, IImportFolderService
    {
        public ImportFolderService(ILogger<ImportFolderService> logger, IImportFolderRepository importFolderRepo) : base(logger, importFolderRepo)
        {
        }

        public ImportFolder GetByLocation(string location)
        {
            return Repo.GetByLocation(location);
        }
    }
}