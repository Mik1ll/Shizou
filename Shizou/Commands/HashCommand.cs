using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Entities;
using Shizou.Extensions;
using Shizou.Hashers;

namespace Shizou.Commands
{
    public record HashParams(string Path) : CommandParams;

    [Command(CommandType.Hash, CommandPriority.Default, QueueType.Hash)]
    public class HashCommand : BaseCommand<HashParams>
    {
        private readonly ShizouContext _context;


        public HashCommand(IServiceProvider provider, HashParams commandParams) : base(provider, provider.GetRequiredService<ILogger<HashCommand>>(),
            commandParams)
        {
            CommandId = $"{nameof(HashCommand)}_{commandParams.Path}";
            _context = provider.GetRequiredService<ShizouContext>();
        }

        public override string CommandId { get; }

        public override async Task Process()
        {
            var file = new FileInfo(CommandParams.Path);
            ImportFolder? importFolder;
            if (!file.Exists || (importFolder = _context.ImportFolders.GetByPath(file.FullName)) is null)
                return;
            var pathTail = file.FullName.Substring(importFolder.Path.Length);
            var signature = RHasher.FileSignature(file.FullName);
            var localFile = _context.LocalFiles.SingleOrDefault(l => l.Signature == signature);
            if (localFile is not null)
            {
                Logger.LogInformation("Found local file by signature: {signature} {Path}", signature, file.FullName);
                var oldPath = Path.GetFullPath(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
                if (file.FullName != oldPath)
                {
                    if (File.Exists(oldPath))
                    {
                        Logger.LogError("Skipping add file for {Path}: duplicate file at {oldPath}.", file.FullName, oldPath);
                        return;
                    }
                    localFile.ImportFolder = importFolder;
                    localFile.PathTail = pathTail;
                }
            }
            else
            {
                Logger.LogInformation("Hashing file: {Path}", file.FullName);
                var hashes = RHasher.GetFileHashes(file.FullName, RHasher.HashIds.Ed2K | RHasher.HashIds.Crc32);
                localFile = new LocalFile
                {
                    Id = localFile?.Id ?? 0,
                    Signature = signature,
                    Crc = hashes[RHasher.HashIds.Crc32],
                    Ed2K = hashes[RHasher.HashIds.Ed2K],
                    FileSize = file.Length,
                    Ignored = false,
                    ImportFolder = importFolder,
                    PathTail = pathTail
                };
                Logger.LogInformation("Hash result: {Path} {Ed2k} {Crc}", file.FullName, hashes[RHasher.HashIds.Ed2K], hashes[RHasher.HashIds.Crc32]);
            }
            _context.LocalFiles.Update(localFile);
            _context.SaveChanges();
            Completed = true;
        }
    }
}
