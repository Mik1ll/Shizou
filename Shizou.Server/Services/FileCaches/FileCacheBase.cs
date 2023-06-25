using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shizou.Server.Services.FileCaches;

public abstract class FileCacheBase<T> where T : class
{
    private readonly string _basePath;
    private readonly TimeSpan _retentionDuration;

    public FileCacheBase(string basePath, TimeSpan retentionDuration)
    {
        _basePath = basePath;
        _retentionDuration = retentionDuration;
    }

    public async Task<T?> Get(string filename)
    {
        var path = Path.Combine(_basePath, filename);
        if (new FileInfo(path) is not { Exists: true, Length: > 0 } fileInfo || fileInfo.LastWriteTimeUtc < DateTime.UtcNow - _retentionDuration) return null;
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
        return await JsonSerializer.DeserializeAsync<T>(file);
    }

    public async Task Save(string filename, T value)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
        var path = Path.Combine(_basePath, filename);
        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(file, value);
    }

    public void Delete(string filename)
    {
        var path = Path.Combine(_basePath, filename);
        if (File.Exists(path))
            File.Delete(path);
    }
}
