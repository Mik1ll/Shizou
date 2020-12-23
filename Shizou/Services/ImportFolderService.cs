using Microsoft.Extensions.Logging;
using Shizou.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Services
{
    interface IImportFolderService
    {
    }
    public class ImportFolderService : IImportFolderService
    {
        private readonly IImportFolderRepository _importFolderRepo;
        private readonly ILogger<ImportFolderService> _logger;
        public ImportFolderService(ILogger<ImportFolderService> logger, IImportFolderRepository importFolderRepo)
        {
            _logger = logger;
            _importFolderRepo = importFolderRepo;
        }
    }
}
