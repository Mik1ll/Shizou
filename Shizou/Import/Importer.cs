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


        public void ScanImportFolder(int importFolderId, bool forceRescan = false)
        {
            var importFolder = _context.ImportFolders.Find(importFolderId);
            var dir = new DirectoryInfo(importFolder.Path);
            var allFiles = dir.GetFiles("*", SearchOption.AllDirectories);
            var filesToHash = allFiles
                .GroupJoin(
                    _context.LocalFiles
                        .Join(_context.ImportFolders,
                            e => e.ImportFolderId,
                            e => e.Id,
                            (file, folder) => new
                            {
                                Path = Path.GetFullPath(Path.Combine(folder.Path, file.PathTail)),
                                file.FileSize,
                                file.Ignored
                            }),
                    e => e.FullName,
                    e => e.Path,
                    (info, dbLocals) => new {FileInfo = info, LocalFile = dbLocals.FirstOrDefault()}
                ).Where(e => !(e.LocalFile?.Ignored ?? false) && (e.FileInfo.Length != e.LocalFile?.FileSize || forceRescan))
                .Select(e => e.FileInfo);

            foreach (var file in filesToHash) _cmdMgr.Dispatch(new HashParams(file.FullName));
            // TODO: finish this function
        }
    }
}
