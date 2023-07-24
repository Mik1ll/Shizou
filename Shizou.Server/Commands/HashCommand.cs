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
using Shizou.Server.Extensions;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public record HashArgs(string Path) : CommandArgs($"{nameof(HashCommand)}_{Path}");

[Command(CommandType.Hash, CommandPriority.Normal, QueueType.Hash)]
public class HashCommand : BaseCommand<HashArgs>
{
    private readonly ILogger<HashCommand> _logger;
    private readonly CommandService _commandService;
    private readonly ShizouContext _context;

    public HashCommand(
        ILogger<HashCommand> logger,
        ShizouContext context,
        CommandService commandService
    )
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
    }

    protected override async Task ProcessInner()
    {
        var file = new FileInfo(CommandArgs.Path);
        ImportFolder? importFolder;
        if (!file.Exists || (importFolder = _context.ImportFolders.GetByPath(file.FullName)) is null)
        {
            _logger.LogWarning("File not found or not inside an import folder: \"{Path}\"", file.FullName);
            return;
        }
        var pathTail = file.FullName.Substring(importFolder.Path.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var signature = RHasherService.GetFileSignature(file.FullName);
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder)
            .FirstOrDefault(l => l.Signature == signature
                                 || (l.ImportFolderId == importFolder.Id && l.PathTail == pathTail));
        if (localFile is not null && localFile.Signature == signature)
        {
            _logger.LogInformation("Found local file by signature: {Signature} \"{Path}\"", signature, file.FullName);
            var oldPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
            if (file.FullName != oldPath)
            {
                if (File.Exists(oldPath))
                {
                    _logger.LogError("Skipping add local file for \"{NewPath}\": duplicate file at \"{OldPath}\"", file.FullName, oldPath);
                    return;
                }
                _logger.LogInformation("Changing path and/or import folder for local file \"{NewPath}\" old path: \"{OldPath}\"", file.FullName, oldPath);
                localFile.ImportFolder = importFolder;
                localFile.PathTail = pathTail;
            }
        }
        else
        {
            if (localFile is not null)
                _logger.LogInformation("Found local file with mismatched signature, rehashing: \"{Path}\"", file.FullName);
            else
                _logger.LogInformation("Hashing new file: \"{Path}\"", file.FullName);
            var hashes = await RHasherService.GetFileHashesAsync(file, RHasherService.HashIds.Ed2K | RHasherService.HashIds.Crc32);
            localFile ??= _context.LocalFiles.Add(new LocalFile
            {
                Signature = signature,
                Crc = hashes[RHasherService.HashIds.Crc32],
                Ed2K = hashes[RHasherService.HashIds.Ed2K],
                FileSize = file.Length,
                Updated = DateTime.UtcNow,
                PathTail = pathTail,
                Ignored = false,
                ImportFolderId = importFolder.Id
            }).Entity;
            _logger.LogInformation("Hash result: \"{Path}\" {Ed2k} {Crc}", file.FullName, hashes[RHasherService.HashIds.Ed2K],
                hashes[RHasherService.HashIds.Crc32]);
        }
        _context.SaveChanges();
        if (_context.AniDbFiles.GetByEd2K(localFile.Ed2K) is null)
            _commandService.Dispatch(new ProcessArgs(localFile.Id, IdTypeLocalFile.LocalId));
        Completed = true;
    }
}
