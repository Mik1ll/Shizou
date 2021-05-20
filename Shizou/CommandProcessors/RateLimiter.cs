using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.CommandProcessors
{
    public abstract class RateLimiter
    {
        private readonly Stopwatch _activeWatch = new();
        private readonly object _lock = new();
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
                                 _watch.Elapsed > ShortDelay && _activeWatch.Elapsed < ShortPeriod;

        public async Task EnsureRate()
        {
            var entered = false;
            while (!entered)
            {
                if (!Available)
                {
                    var nextAvailable = (_activeWatch.Elapsed > ShortPeriod ? LongDelay : ShortDelay) - _watch.Elapsed;
                    Logger.LogDebug("Time since last command: {watchElapsed}, waiting for {nextAvailable}", _watch.Elapsed, nextAvailable);
                    await Task.Delay(nextAvailable);
                }
                lock (_lock)
                {
                    if (Available)
                    {
                        if (_watch.Elapsed > ResetPeriod)
                            _activeWatch.Restart();
                        _watch.Restart();
                        entered = true;
                        Logger.LogDebug("Got rate limiter");
                    }
                }
            }
        }
    }
}
