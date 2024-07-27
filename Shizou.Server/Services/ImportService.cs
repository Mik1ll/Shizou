using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class ImportService
{
    private readonly CommandService _commandService;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly ILogger<ImportService> _logger;
    private readonly IShizouContextFactory _contextFactory;

    public ImportService(ILogger<ImportService> logger, IShizouContextFactory contextFactory, CommandService commandService,
        IOptionsMonitor<ShizouOptions> optionsMonitor)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _commandService = commandService;
        _optionsMonitor = optionsMonitor;
    }


    /// <summary>
    ///     Scans each import folder with the scan on import flag set.
    /// </summary>
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
        if (!dir.Exists)
        {
            _logger.LogError("Import folder path \"{ImportPath}\" does not exist", importFolder.Path);
            return;
        }

        var extensions = _optionsMonitor.CurrentValue.Import.FileExtensions.Select(ext => ext.TrimStart('.')).ToArray();
        var dbFiles = context.LocalFiles.Include(lf => lf.ImportFolder)
            .Where(lf => lf.ImportFolder != null)
            .ToDictionary(lf => Path.Combine(lf.ImportFolder!.Path, lf.PathTail));
        var filesToHash = dir.EnumerateFiles("*", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        }).Where(f => f.Length > 0 && extensions.Contains(f.Extension.TrimStart('.'), StringComparer.OrdinalIgnoreCase) &&
                      (!dbFiles.TryGetValue(f.FullName, out var lf) || (!lf.Ignored && (forceRescan || f.Length != lf.FileSize))));

        _commandService.DispatchRange(filesToHash.Select(e => new HashArgs(e.FullName)));
    }

    /// <summary>
    ///     Removes the Local File entries for files missing from storage.
    /// </summary>
    public void RemoveMissingFiles()
    {
        _logger.LogInformation("Removing missing local files");
        using var context = _contextFactory.CreateDbContext();
        var localFiles = context.LocalFiles.Include(lf => lf.ImportFolder).ToList();
        var toRemove = localFiles.Where(file => file.ImportFolder is null || !File.Exists(Path.Combine(file.ImportFolder.Path, file.PathTail))).ToList();
        foreach (var file in toRemove)
        {
            if (file.ImportFolder is null)
                _logger.LogInformation("Removing local file with missing import folder: \"{FileName}\"", Path.GetFileName(file.PathTail));
            else
                _logger.LogInformation("Removing missing local file: \"{FilePath}\"", Path.Combine(file.ImportFolder.Path, file.PathTail));
            context.LocalFiles.Remove(file);
        }

        context.SaveChanges();
    }

    /// <summary>
    ///     Remove a specific local file entry by it's ID.
    /// </summary>
    /// <param name="localFileId"></param>
    public void RemoveLocalFile(int localFileId)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFile = context.LocalFiles.FirstOrDefault(lf => lf.Id == localFileId);
        if (localFile is not null)
        {
            context.LocalFiles.Remove(localFile);
            context.SaveChanges();
            _logger.LogInformation("Removed local file with id: {LocalFileId}, PathTail: {PathTail}", localFileId, localFile.PathTail);
        }
        else
        {
            _logger.LogWarning("Tried to remove non-existant local file with id {LocalFileId}", localFileId);
        }
    }

    /// <summary>
    ///     Set the ignored state for a list of files. Ignored files will not be hashed/scanned.
    /// </summary>
    /// <param name="localFileIds"></param>
    /// <param name="ignored"></param>
    public void SetIgnored(IEnumerable<int> localFileIds, bool ignored)
    {
        using var context = _contextFactory.CreateDbContext();
        foreach (var fileId in localFileIds)
        {
            var localFile = context.LocalFiles.Find(fileId);
            if (localFile is null || localFile.Ignored == ignored)
                continue;
            _logger.LogInformation("Setting ignored state {State} for local file: {LocalFileId} filename: \"{FileName}\"", ignored, fileId,
                Path.GetFileName(localFile.PathTail));
            localFile.Ignored = ignored;
        }

        context.SaveChanges();
    }
}
