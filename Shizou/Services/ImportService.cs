using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shizou.Commands;
using Shizou.Database;

namespace Shizou.Services;

public class ImportService
{
    private readonly CommandManager _cmdMgr;
    private readonly ShizouContext _context;
    private readonly ILogger<ImportService> _logger;

    public ImportService(ILogger<ImportService> logger, ShizouContext context, CommandManager cmdMgr)
    {
        _logger = logger;
        _context = context;
        _cmdMgr = cmdMgr;
    }


    public void Import()
    {
        _logger.LogInformation("Beginning import");
        var folders = _context.ImportFolders.Where(f => f.ScanOnImport);
        foreach (var folder in folders)
            ScanImportFolder(folder.Id);
    }


    /// <summary>
    ///     Scan import folder for new/moved files.
    /// </summary>
    /// <param name="importFolderId">Id of the import folder</param>
    /// <param name="forceRescan">Ensures even files with matching file size are rehashed</param>
    public void ScanImportFolder(int importFolderId, bool forceRescan = false)
    {
        var importFolder = _context.ImportFolders.Find(importFolderId);
        if (importFolder is null)
            throw new InvalidOperationException($"import folder id {importFolderId} not found");
        _logger.LogInformation("Beginning scan on import folder \"{importFolderPath}\"", importFolder.Path);
        var dir = new DirectoryInfo(importFolder.Path);
        var allFiles = dir.GetFiles("*", SearchOption.AllDirectories);
        var filesToHash = allFiles
            // Left Outer Join on all files with DB local files. Includes DB files that are not ignored and mismatch filesize.
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
                (info, dbLocals) => new { FileInfo = info, LocalFile = dbLocals.FirstOrDefault() })
            .Where(e => !(e.LocalFile?.Ignored ?? false) && (e.FileInfo.Length != e.LocalFile?.FileSize || forceRescan))
            .Select(e => e.FileInfo);

        _cmdMgr.DispatchRange(filesToHash.Select(e => new HashParams(e.FullName)));
    }

    public void CheckForMissingFiles()
    {
        var files = _context.LocalFiles.Join(_context.ImportFolders, e => e.ImportFolderId, e => e.Id,
                (file, folder) => new { Path = Path.GetFullPath(Path.Combine(folder.Path, file.PathTail)), LocalFile = file })
            .Where(e => !new FileInfo(e.Path).Exists).Select(e => e.LocalFile);
    }
}