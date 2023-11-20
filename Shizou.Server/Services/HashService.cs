using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shizou.Server.RHash;

namespace Shizou.Server.Services;

public class HashService
{
    /// <summary>
    ///     Get file hashes
    /// </summary>
    /// <param name="file">File to hash</param>
    /// <param name="hashIds">Hashes to retrieve</param>
    /// <returns>Dictionary of hash results</returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<Dictionary<HashIds, string>> GetFileHashesAsync(FileInfo file, HashIds hashIds)
    {
        if (!file.Exists)
            throw new FileNotFoundException("Couldn't find file when trying to get file hash");
        var hasher = new RHasher(hashIds);
        var bufSize = 1 << 20;
        var stream = file.OpenRead();
        await using var _ = stream.ConfigureAwait(false);
        var buf = new byte[bufSize];
        int len;
        while ((len = await stream.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0)
            hasher.Update(buf, len);

        hasher.Finish();
        return Enum.GetValues(typeof(HashIds)).Cast<HashIds>().Where(id => hashIds.HasFlag(id))
            .ToDictionary(id => id, id => hasher.ToString(id));
    }

    /// <summary>
    ///     Get file signature/thumbprint
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<string> GetFileSignatureAsync(FileInfo file)
    {
        if (!file.Exists)
            throw new FileNotFoundException("Couldn't find file when trying to get file signature");
        var bufSize = 1 << 20;
        var seekLen = Math.Max(file.Length / 30 - bufSize, 0);
        var hasher = new RHasher(HashIds.Sha1);
        var stream = file.OpenRead();
        await using var _ = stream.ConfigureAwait(false);
        var buf = new byte[bufSize];
        int len;
        while ((len = await stream.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0)
        {
            hasher.Update(buf, len);
            stream.Seek(seekLen, SeekOrigin.Current);
        }

        return hasher.Finish().ToString();
    }
}
