using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.RateLimiters;

public abstract class RateLimiter
{
    private readonly Stopwatch _activeWatch = new();
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

    public async Task<IDisposable> AcquireAsync()
    {
        Logger.LogTrace("Waiting on rate limiter");
        await _rateSemaphore.WaitAsync().ConfigureAwait(false);
        Logger.LogTrace("Got rate limiter");
        if (!Available && NextAvailable - DateTimeOffset.UtcNow is var timeUntilNext && timeUntilNext > TimeSpan.Zero)
        {
            Logger.LogDebug("Time since last command: {WatchElapsed}, waiting for {TimeUntilNext}", _watch.Elapsed, timeUntilNext);
            await Task.Delay(timeUntilNext).ConfigureAwait(false);
        }

        return new ReleaseWrapper(this);
    }

    private void Release()
    {
        if (_rateSemaphore.CurrentCount != 0)
            return;
        if (_watch.Elapsed > ResetPeriod)
            _activeWatch.Restart();
        _watch.Restart();
        NextAvailable = DateTimeOffset.UtcNow + (_activeWatch.Elapsed > ShortPeriod ? LongDelay : ShortDelay);
        Logger.LogTrace("Rate limiter released");
        _rateSemaphore.Release();
    }

    private class ReleaseWrapper : IDisposable
    {
        private readonly RateLimiter _rateLimiter;
        private bool _isDisposed;

        public ReleaseWrapper(RateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _rateLimiter.Release();
            _isDisposed = true;
        }
    }
}
