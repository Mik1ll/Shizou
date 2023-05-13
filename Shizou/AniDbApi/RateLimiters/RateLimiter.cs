using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.RateLimiters;

public abstract class RateLimiter
{
    private readonly Stopwatch _activeWatch = new();
    private readonly object _lock = new();
    private readonly SemaphoreSlim _rateSemaphore = new(1, 1);
    private readonly Stopwatch _watch = new();
    protected readonly ILogger<RateLimiter> Logger;

    protected RateLimiter(ILogger<RateLimiter> logger)
    {
        Logger = logger;
        _watch.Start();
    }

    protected abstract TimeSpan ShortDelay { get; }
    protected abstract TimeSpan LongDelay { get; }
    protected abstract TimeSpan ShortPeriod { get; }
    protected abstract TimeSpan ResetPeriod { get; }

    public bool Available => _watch.Elapsed > LongDelay ||
                             (_watch.Elapsed > ShortDelay && _activeWatch.Elapsed < ShortPeriod);

    public DateTimeOffset NextAvailable { get; private set; } = DateTimeOffset.UtcNow;

    public async Task EnsureRate()
    {
        try
        {
            await _rateSemaphore.WaitAsync();
            if (!Available)
            {
                Logger.LogDebug("Time since last command: {WatchElapsed}, waiting for {NextAvailable}", _watch.Elapsed, NextAvailable - DateTimeOffset.UtcNow);
                await Task.Delay(NextAvailable - DateTimeOffset.UtcNow);
            }
            if (_watch.Elapsed > ResetPeriod)
                _activeWatch.Restart();
            _watch.Restart();
            NextAvailable = DateTimeOffset.UtcNow + (_activeWatch.Elapsed > ShortPeriod ? LongDelay : ShortDelay);
            Logger.LogDebug("Got rate limiter");
        }
        finally
        {
            _rateSemaphore.Release();
        }
    }
}