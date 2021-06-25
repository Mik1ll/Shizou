using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shizou.Commands;
using Shizou.Database;

namespace Shizou.Import
{
    public class Importer
    {
        private readonly CommandManager _cmdMgr;
        private readonly ShizouContext _context;
        private readonly ILogger<Importer> _logger;

        public Importer(ILogger<Importer> logger, ShizouContext context, CommandManager cmdMgr)
        {
            _logger = logger;
            _context = context;
            _cmdMgr = cmdMgr;
        }


        public void ScanImportFolder(int importFolderId)
        {
            var importFolder = _context.ImportFolders.Find(importFolderId);
            var dir = new DirectoryInfo(importFolder.Location);
            var allFiles = dir.GetFiles("*", SearchOption.AllDirectories).ToList();
            foreach (var file in allFiles) _cmdMgr.Dispatch(new HashParams(file.FullName));
            // TODO: finish this function
        }
    }
}
