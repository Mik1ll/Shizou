using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.FileCaches;

public abstract class FileCacheBase<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly ILogger<FileCacheBase<TIn, TOut>> _logger;
    private readonly string _basePath;
    private readonly TimeSpan _retentionDuration;

    protected FileCacheBase(ILogger<FileCacheBase<TIn, TOut>> logger, string basePath, TimeSpan retentionDuration)
    {
        _logger = logger;
        _basePath = basePath;
        _retentionDuration = retentionDuration;
    }

    public async Task<TOut?> GetAsync(string filename)
    {
        var path = Path.Combine(_basePath, filename);
        if (!InsideRetentionPeriod(filename))
        {
            _logger.LogDebug("{Path} is outside retention period", path);
            return null;
        }

        if (new FileInfo(path) is not { Exists: true, Length: > 0 })
        {
            _logger.LogDebug("{Path} not found in cache", path);
            return null;
        }

        _logger.LogDebug("{Path} found in cache", path);
        var file = new FileStream(path, FileMode.Open, FileAccess.Read);
        await using var _ = file.ConfigureAwait(false);
        return await DeserializeAsync(file).ConfigureAwait(false);
    }

    public async Task SaveAsync(string filename, TIn value)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
        var path = Path.Combine(_basePath, filename);
        var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        await using var _ = file.ConfigureAwait(false);
        await SerializeAsync(value, file).ConfigureAwait(false);
        _logger.LogDebug("{Path} saved to cache", path);
    }

    public void Delete(string filename)
    {
        var path = Path.Combine(_basePath, filename);
        if (File.Exists(path))
            File.Delete(path);
    }

    protected virtual async Task<TOut?> DeserializeAsync(FileStream file)
    {
        return await JsonSerializer.DeserializeAsync<TOut>(file).ConfigureAwait(false);
    }

    protected virtual Task SerializeAsync(TIn value, FileStream file)
    {
        return JsonSerializer.SerializeAsync(file, value);
    }

    private bool InsideRetentionPeriod(string filename)
    {
        var path = Path.Combine(_basePath, filename);
        var fileInfo = new FileInfo(path);
        return !fileInfo.Exists || fileInfo.LastWriteTimeUtc > DateTime.UtcNow - _retentionDuration;
    }
}
