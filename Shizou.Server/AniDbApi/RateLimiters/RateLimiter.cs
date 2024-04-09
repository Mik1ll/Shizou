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
    private readonly ILogger<RateLimiter> _logger;

    private DateTimeOffset _nextAvailable = DateTimeOffset.UtcNow;

    protected RateLimiter(ILogger<RateLimiter> logger) => _logger = logger;

    protected abstract TimeSpan ShortDelay { get; }
    protected abstract TimeSpan LongDelay { get; }
    protected abstract TimeSpan ShortPeriod { get; }
    protected abstract TimeSpan ResetPeriod { get; }

    public async Task<IDisposable> AcquireAsync()
    {
        _logger.LogTrace("Waiting on rate limiter");
        await _rateSemaphore.WaitAsync().ConfigureAwait(false);
        _logger.LogTrace("Got rate limiter");
        var timeUntilNext = _nextAvailable - DateTimeOffset.UtcNow;
        if (timeUntilNext > TimeSpan.Zero)
        {
            _logger.LogDebug("Time since last command: {WatchElapsed}, waiting for {TimeUntilNext}", _watch.Elapsed, timeUntilNext);
            await Task.Delay(timeUntilNext).ConfigureAwait(false);
        }

        return new ReleaseWrapper(this);
    }

    private void Release()
    {
        if (_rateSemaphore.CurrentCount != 0)
            return;
        if (_watch.Elapsed > ResetPeriod)
            _activeWatch.Reset();
        _activeWatch.Start();
        _watch.Restart();
        _nextAvailable = DateTimeOffset.UtcNow + (_activeWatch.Elapsed < ShortPeriod ? ShortDelay : LongDelay);
        _logger.LogTrace("Rate limiter released");
        _rateSemaphore.Release();
    }

    private class ReleaseWrapper : IDisposable
    {
        private readonly RateLimiter _rateLimiter;
        private bool _isDisposed;

        public ReleaseWrapper(RateLimiter rateLimiter) => _rateLimiter = rateLimiter;

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _rateLimiter.Release();
            _isDisposed = true;
        }
    }
}
