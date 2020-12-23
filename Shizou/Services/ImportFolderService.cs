using Microsoft.Extensions.Logging;
using Shizou.Entities;
using Shizou.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Services
{
    public interface IImportFolderService : ISingleRepoService<ImportFolder>
    {
    }
    public class ImportFolderService : BaseSingleRepoService<IImportFolderRepository, ImportFolder>, IImportFolderService
    {
        public ImportFolderService(ILogger<ImportFolderService> logger, IImportFolderRepository importFolderRepo) : base(logger, importFolderRepo)
        {
        }
    }
}
