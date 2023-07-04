using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Server.Commands;

namespace Shizou.Server.Services;

public class ImportService
{
    private readonly CommandService _commandService;
    private readonly ILogger<ImportService> _logger;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

    public ImportService(ILogger<ImportService> logger, IDbContextFactory<ShizouContext> contextFactory, CommandService commandService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _commandService = commandService;
    }


    public void Import()
    {
        _logger.LogInformation("Beginning import");
        using var context = _contextFactory.CreateDbContext();
        var folders = context.ImportFolders.Where(f => f.ScanOnImport);
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
        using var context = _contextFactory.CreateDbContext();
        var importFolder = context.ImportFolders.Find(importFolderId);
        if (importFolder is null)
            throw new InvalidOperationException($"Import folder id {importFolderId} not found");
        _logger.LogInformation("Beginning scan on import folder \"{ImportFolderPath}\"", importFolder.Path);
        var dir = new DirectoryInfo(importFolder.Path);
        var allFiles = dir.GetFiles("*", SearchOption.AllDirectories);
        var dbFiles = context.LocalFiles.Include(lf => lf.ImportFolder)
            .ToDictionary(lf => Path.Combine(lf.ImportFolder.Path, lf.PathTail));
        var filesToHash = allFiles.Where(f =>
            !dbFiles.TryGetValue(f.FullName, out var lf) || (!lf.Ignored && (forceRescan || f.Length != lf.FileSize)));

        _commandService.DispatchRange(filesToHash.Select(e => new HashArgs(e.FullName)));
    }

    public void CheckForMissingFiles()
    {
        using var context = _contextFactory.CreateDbContext();
        var files = context.LocalFiles.Join(context.ImportFolders, e => e.ImportFolderId, e => e.Id,
                (file, folder) => new { Path = Path.GetFullPath(Path.Combine(folder.Path, file.PathTail)), LocalFile = file })
            .Where(e => !new FileInfo(e.Path).Exists).Select(e => e.LocalFile);
    }
}
