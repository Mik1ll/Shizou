using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public record HashArgs(string Path) : CommandArgs($"{nameof(HashCommand)}_{Path}");

[Command(CommandType.Hash, CommandPriority.Normal, QueueType.Hash)]
public class HashCommand : BaseCommand<HashArgs>
{
    private readonly ILogger<HashCommand> _logger;
    private readonly CommandService _commandService;
    private readonly ShizouContext _context;
    private readonly ShizouOptions _options;

    public HashCommand(
        ILogger<HashCommand> logger,
        ShizouContext context,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot
    )
    {
        _logger = logger;
        _context = context;
        _commandService = commandService;
        _options = optionsSnapshot.Value;
    }

    protected override async Task ProcessInner()
    {
        var file = new FileInfo(CommandArgs.Path);
        var importFolder = _context.ImportFolders.GetByPath(file.FullName);
        if (!file.Exists || importFolder is null)
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
            var oldPath = localFile.ImportFolder is null ? null : Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
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
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (localFile is not null)
                _logger.LogInformation("Found local file with mismatched signature, rehashing: \"{Path}\"", file.FullName);
            else
                _logger.LogInformation("Hashing new file: \"{Path}\"", file.FullName);
            var hashes = await RHasherService.GetFileHashesAsync(file, RHasherService.HashIds.Ed2k | RHasherService.HashIds.Crc32);
            localFile ??= _context.LocalFiles.Add(new LocalFile
            {
                Signature = signature,
                Crc = hashes[RHasherService.HashIds.Crc32],
                Ed2k = hashes[RHasherService.HashIds.Ed2k],
                FileSize = file.Length,
                Updated = DateTime.UtcNow,
                PathTail = pathTail,
                Ignored = false,
                ImportFolderId = importFolder.Id
            }).Entity;
            _logger.LogInformation("Hash result: \"{Path}\" {Ed2k} {Crc}", file.FullName, hashes[RHasherService.HashIds.Ed2k],
                hashes[RHasherService.HashIds.Crc32]);
        }
        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();
        var eAniDbFileId = _context.AniDbFiles.Where(f => f.Ed2k == localFile.Ed2k).Select(f => (int?)f.Id).FirstOrDefault();
        if (eAniDbFileId is null)
            _commandService.Dispatch(new ProcessArgs(localFile.Id, IdTypeLocalFile.LocalId));
        Completed = true;
    }
}
