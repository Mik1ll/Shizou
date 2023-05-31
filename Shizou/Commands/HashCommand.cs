﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Commands.AniDb;
using Shizou.Extensions;
using Shizou.Services;
using ShizouCommon.Enums;
using ShizouData.Database;
using ShizouData.Models;

namespace Shizou.Commands;

public record HashArgs(string Path) : CommandArgs($"{nameof(HashCommand)}_{Path}");

[Command(CommandType.Hash, CommandPriority.Normal, QueueType.Hash)]
public class HashCommand : BaseCommand<HashArgs>
{
    private readonly CommandService _commandService;
    private readonly ShizouContext _context;


    public HashCommand(IServiceProvider provider, HashArgs commandArgs) : base(provider, commandArgs)
    {
        _context = provider.GetRequiredService<ShizouContext>();
        _commandService = provider.GetRequiredService<CommandService>();
    }

    public override async Task Process()
    {
        var file = new FileInfo(CommandArgs.Path);
        ImportFolder? importFolder;
        if (!file.Exists || (importFolder = _context.ImportFolders.GetByPath(file.FullName)) is null)
        {
            Logger.LogWarning("File not found or not inside an import folder: \"{Path}\"", file.FullName);
            return;
        }
        var pathTail = file.FullName.Substring(importFolder.Path.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var signature = RHasherService.GetFileSignature(file.FullName);
        var localFile = _context.LocalFiles.Include(e => e.ImportFolder)
            .FirstOrDefault(l => l.Signature == signature
                                 || (l.ImportFolderId == importFolder.Id && l.PathTail == pathTail));
        if (localFile is not null && localFile.Signature == signature)
        {
            Logger.LogInformation("Found local file by signature: {Signature} \"{Path}\"", signature, file.FullName);
            var oldPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
            if (file.FullName != oldPath)
            {
                if (File.Exists(oldPath))
                {
                    Logger.LogError("Skipping add local file for \"{NewPath}\": duplicate file at \"{OldPath}\"", file.FullName, oldPath);
                    return;
                }
                Logger.LogInformation("Changing path and/or import folder for local file \"{NewPath}\" old path: \"{OldPath}\"", file.FullName, oldPath);
                localFile.ImportFolder = importFolder;
                localFile.PathTail = pathTail;
            }
        }
        else
        {
            if (localFile is not null)
                Logger.LogInformation("Found local file with mismatched signature, rehashing: \"{Path}\"", file.FullName);
            else
                Logger.LogInformation("Hashing new file: \"{Path}\"", file.FullName);
            var hashes = await RHasherService.GetFileHashesAsync(file, RHasherService.HashIds.Ed2K | RHasherService.HashIds.Crc32);
            localFile ??= _context.LocalFiles.Add(new LocalFile
            {
                Signature = signature,
                Crc = hashes[RHasherService.HashIds.Crc32],
                Ed2K = hashes[RHasherService.HashIds.Ed2K],
                FileSize = file.Length,
                Updated = DateTimeOffset.UtcNow,
                PathTail = pathTail,
                Ignored = false,
                ImportFolderId = importFolder.Id
            }).Entity;
            Logger.LogInformation("Hash result: \"{Path}\" {Ed2k} {Crc}", file.FullName, hashes[RHasherService.HashIds.Ed2K],
                hashes[RHasherService.HashIds.Crc32]);
        }
        _context.SaveChanges();
        if (_context.AniDbFiles.GetByEd2K(localFile.Ed2K) is null)
            _commandService.Dispatch(new ProcessArgs(localFile.Id, IdType.LocalId));
        Completed = true;
    }
}
