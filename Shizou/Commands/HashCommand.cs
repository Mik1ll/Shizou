using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Commands.AniDb;
using Shizou.Database;
using Shizou.Entities;
using Shizou.Extensions;
using Shizou.Services;

namespace Shizou.Commands
{
    public record HashParams(string Path) : CommandParams($"{nameof(HashCommand)}_{Path}");

    [Command(CommandType.Hash, CommandPriority.Default, QueueType.Hash)]
    public class HashCommand : BaseCommand<HashParams>
    {
        private readonly CommandManager _cmdMgr;
        private readonly ShizouContext _context;


        public HashCommand(IServiceProvider provider, HashParams commandParams) : base(provider, provider.GetRequiredService<ILogger<HashCommand>>(),
            commandParams)
        {
            _context = provider.GetRequiredService<ShizouContext>();
            _cmdMgr = provider.GetRequiredService<CommandManager>();
        }

        public override async Task Process()
        {
            var file = new FileInfo(CommandParams.Path);
            ImportFolder? importFolder;
            if (!file.Exists || (importFolder = _context.ImportFolders.GetByPath(file.FullName)) is null)
            {
                Logger.LogWarning("File not found or not inside an import folder: \"{Path}\"", file.FullName);
                return;
            }
            var pathTail = file.FullName.Substring(importFolder.Path.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var signature = RHasher.GetFileSignature(file.FullName);
            var localFile = _context.LocalFiles.Include(e => e.ImportFolder)
                .FirstOrDefault(l => l.Signature == signature
                                     || l.ImportFolderId == importFolder.Id && l.PathTail == pathTail);
            if (localFile is not null && localFile.Signature == signature)
            {
                Logger.LogInformation("Found local file by signature: {signature} \"{Path}\"", signature, file.FullName);
                var oldPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
                if (file.FullName != oldPath)
                {
                    if (File.Exists(oldPath))
                    {
                        Logger.LogError("Skipping add local file for \"{newPath}\": duplicate file at \"{oldPath}\"", file.FullName, oldPath);
                        return;
                    }
                    Logger.LogInformation("Changing path and/or import folder for local file \"{newPath}\" old path: \"{oldPath}\"", file.FullName, oldPath);
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
                var hashes = await RHasher.GetFileHashesAsync(file, RHasher.HashIds.Ed2K | RHasher.HashIds.Crc32);
                int? existingAniDbFileId =
                    _context.AniDbFiles.Where(f => f.Ed2K == hashes[RHasher.HashIds.Ed2K]).Select(f => f.Id).FirstOrDefault() is var fileId
                    && fileId != 0
                        ? fileId
                        : null;
                if (localFile is null)
                    localFile = _context.LocalFiles.Add(new LocalFile()).Entity;
                localFile.Signature = signature;
                localFile.Crc = hashes[RHasher.HashIds.Crc32];
                localFile.Ed2K = hashes[RHasher.HashIds.Ed2K];
                localFile.FileSize = file.Length;
                localFile.Updated = DateTime.UtcNow;
                localFile.ImportFolder = importFolder;
                localFile.PathTail = pathTail;
                localFile.AniDbFileId = existingAniDbFileId;
                Logger.LogInformation("Hash result: \"{Path}\" {Ed2k} {Crc}", file.FullName, hashes[RHasher.HashIds.Ed2K], hashes[RHasher.HashIds.Crc32]);
            }
            _context.SaveChanges();
            if (localFile.AniDbFileId is null)
                _cmdMgr.Dispatch(new ProcessParams(localFile.Id));
            Completed = true;
        }
    }
}
