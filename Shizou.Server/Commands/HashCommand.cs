using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions.Query;
using Shizou.Server.RHash;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public record HashArgs(string Path) : CommandArgs($"{nameof(HashCommand)}_{Path}",
    typeof(HashCommand), CommandPriority.Normal, QueueType.Hash);

public class HashCommand : Command<HashArgs>
{
    private readonly ILogger<HashCommand> _logger;
    private readonly CommandService _commandService;
    private readonly HashService _hashService;
    private readonly IShizouContext _context;

    public HashCommand(
        ILogger<HashCommand> logger,
        IShizouContext context,
        CommandService commandService,
        HashService hashService
    )
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _hashService = hashService;
    }

    protected override async Task ProcessInnerAsync()
    {
        var file = new FileInfo(CommandArgs.Path);
        var importFolder = _context.ImportFolders.ByPath(file.FullName);
        if (!file.Exists || importFolder is null)
        {
            _logger.LogWarning("File not found or not inside an import folder: \"{Path}\"", file.FullName);
            Completed = true;
            return;
        }

        var pathTail = file.FullName.Substring(importFolder.Path.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var signature = await _hashService.GetFileSignatureAsync(file).ConfigureAwait(false);
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder)
            .FirstOrDefault(l => l.Signature == signature
                                 || (l.ImportFolderId == importFolder.Id && l.PathTail == pathTail));
        if (localFile is not null && localFile.Signature == signature)
        {
            _logger.LogInformation("Found local file by signature: {Signature} \"{Path}\"", signature, file.FullName);
            var oldPath = localFile.ImportFolder is null ? null : Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
            if (file.FullName != oldPath)
            {
                if (File.Exists(oldPath))
                {
                    _logger.LogError("Skipping add local file for \"{NewPath}\": duplicate file at \"{OldPath}\"", file.FullName, oldPath);
                    Completed = true;
                    return;
                }

                _logger.LogInformation("Changing path and/or import folder for local file \"{NewPath}\" old path: \"{OldPath}\"", file.FullName, oldPath);
                localFile.ImportFolder = importFolder;
                localFile.PathTail = pathTail;
            }
        }
        else
        {
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (localFile is not null)
                _logger.LogInformation("Found local file with mismatched signature, rehashing: \"{Path}\"", file.FullName);
            else
                _logger.LogInformation("Hashing new file: \"{Path}\"", file.FullName);
            var hashes = await _hashService.GetFileHashesAsync(file, HashIds.Ed2k | HashIds.Crc32).ConfigureAwait(false);
            var newLocalFile = new LocalFile
            {
                Id = localFile?.Id ?? 0,
                Signature = signature,
                Crc = hashes[HashIds.Crc32],
                Ed2k = hashes[HashIds.Ed2k],
                FileSize = file.Length,
                Updated = DateTime.UtcNow,
                PathTail = pathTail,
                Ignored = false,
                ImportFolderId = importFolder.Id
            };
            if (localFile is null)
                localFile = _context.LocalFiles.Add(newLocalFile).Entity;
            else
                _context.Entry(localFile).CurrentValues.SetValues(newLocalFile);
            _logger.LogInformation("Hash result: \"{Path}\" {Ed2k} {Crc}", file.FullName, hashes[HashIds.Ed2k],
                hashes[HashIds.Crc32]);
        }

        var eAniDbFileId = _context.AniDbFiles.Where(f => f.Ed2k == localFile.Ed2k).Select(f => new { f.Id }).FirstOrDefault()?.Id;
        localFile.AniDbFileId = eAniDbFileId;
        _context.SaveChanges();

        if (eAniDbFileId is null)
            _commandService.Dispatch(new ProcessArgs(localFile.Id, IdTypeLocalFile.LocalId));
        Completed = true;
    }
}
