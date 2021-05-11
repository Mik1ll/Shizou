using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.CommandProcessors
{
    public abstract class RateLimiter
    {
        protected readonly ILogger<RateLimiter> Logger;

        protected RateLimiter(ILogger<RateLimiter> logger)
        {
            Logger = logger;
            _watch.Start();
            _activeWatch.Start();
        }

        private readonly object _lock = new();
        protected abstract TimeSpan ShortDelay { get; }
        protected abstract TimeSpan LongDelay { get; }
        protected abstract TimeSpan ShortPeriod { get; }
        protected abstract TimeSpan ResetPeriod { get; }
        private readonly Stopwatch _watch = new();
        private readonly Stopwatch _activeWatch = new();

        public bool Available => _watch.Elapsed > LongDelay ||
                                 _watch.Elapsed > ShortDelay && (_activeWatch.Elapsed < ShortPeriod || _activeWatch.Elapsed > ResetPeriod);

        public TimeSpan NextAvailable => Available ? new TimeSpan(0) : (_activeWatch.Elapsed > ShortPeriod ? LongDelay : ShortDelay) - _watch.Elapsed;

        public async void Wait()
        {
            var entered = false;
            while (!entered)
            {
                if (!Available)
                {
                    Logger.LogDebug($"Time since last command: {_watch.Elapsed}, waiting for {NextAvailable}");
                    await Task.Delay(NextAvailable);
                }
                lock (_lock)
                {
                    if (Available)
                    {
                        if (_watch.Elapsed > ResetPeriod)
                            _activeWatch.Restart();
                        _watch.Restart();
                        entered = true;
                    }
                }
            }
        }
    }
}
