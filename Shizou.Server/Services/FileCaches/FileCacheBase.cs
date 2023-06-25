using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.Services.FileCaches;

public abstract class FileCacheBase<TIn, TOut>
    where TIn : class
    where TOut : class
{
    protected readonly ILogger<FileCacheBase<TIn, TOut>> Logger;

    public FileCacheBase(ILogger<FileCacheBase<TIn, TOut>> logger, string basePath, TimeSpan retentionDuration)
    {
        Logger = logger;
        BasePath = basePath;
        RetentionDuration = retentionDuration;
    }

    public string BasePath { get; }

    public TimeSpan RetentionDuration { get; }

    public async Task<TOut?> Get(string filename)
    {
        var path = Path.Combine(BasePath, filename);
        if (!InsideRetentionPeriod(filename))
        {
            Logger.LogDebug("{Path} is outside retention period", path);
            return null;
        }
        if (new FileInfo(path) is not { Exists: true, Length: > 0 })
        {
            Logger.LogDebug("{Path} not found in cache", path);
            return null;
        }
        Logger.LogDebug("{Path} found in cache", path);
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
        return await DeserializeAsync(file);
    }

    public bool InsideRetentionPeriod(string filename)
    {
        var path = Path.Combine(BasePath, filename);
        var fileInfo = new FileInfo(path);
        return !fileInfo.Exists || fileInfo.LastWriteTimeUtc > DateTime.UtcNow - RetentionDuration;
    }

    public async Task Save(string filename, TIn value)
    {
        if (!Directory.Exists(BasePath))
            Directory.CreateDirectory(BasePath);
        var path = Path.Combine(BasePath, filename);
        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        await SerializeAsync(value, file);
        Logger.LogDebug("{Path} saved to cache", path);
    }

    public void Delete(string filename)
    {
        var path = Path.Combine(BasePath, filename);
        if (File.Exists(path))
            File.Delete(path);
    }

    protected virtual async Task<TOut?> DeserializeAsync(FileStream file)
    {
        return await JsonSerializer.DeserializeAsync<TOut>(file);
    }

    protected virtual Task SerializeAsync(TIn value, FileStream file)
    {
        return JsonSerializer.SerializeAsync(file, value);
    }
}
