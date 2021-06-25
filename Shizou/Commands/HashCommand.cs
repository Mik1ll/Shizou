using System;
using System.Collections.Generic;
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


        public HashCommand(IServiceProvider provider, ILogger<BaseCommand<HashParams>> logger, HashParams commandParams) : base(provider, logger, commandParams)
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
            var pathTail = file.FullName.Substring(importFolder.Location.Length);
            var oldLocalFile = _context.LocalFiles.SingleOrDefault(l => l.Signature == RHasher.FileSignature(file.FullName));
            if (oldLocalFile is not null)
            {
                var oldPath = Path.GetFullPath(Path.Combine(oldLocalFile.ImportFolder.Location, oldLocalFile.PathTail));
                if (file.FullName != oldPath && File.Exists(oldPath))
                {
                    Logger.LogError("Skipping add local file for {Path}: duplicate file at {oldPath}.", file.FullName, oldPath);
                    return;
                }
            }
            var hashes = oldLocalFile is null
                ? RHasher.GetFileHashes(file.FullName, RHasher.HashIds.Ed2K | RHasher.HashIds.Crc32)
                : new Dictionary<RHasher.HashIds, string>
                {
                    {RHasher.HashIds.Crc32, oldLocalFile.Crc},
                    {RHasher.HashIds.Ed2K, oldLocalFile.Ed2K}
                };
            _context.LocalFiles.Update(new LocalFile
            {
                Id = oldLocalFile?.Id ?? 0,
                Crc = hashes[RHasher.HashIds.Crc32],
                Ed2K = hashes[RHasher.HashIds.Ed2K],
                Created = file.CreationTimeUtc,
                Modified = file.LastWriteTimeUtc,
                FileSize = file.Length,
                Ignored = false,
                ImportFolder = importFolder,
                PathTail = pathTail
            });
            Completed = true;
        }
    }
}
