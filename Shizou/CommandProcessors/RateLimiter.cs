using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Shizou.CommandProcessors
{
    public abstract class RateLimiter
    {
        protected readonly ILogger<RateLimiter> _logger;
        
        protected RateLimiter(ILogger<RateLimiter> logger)
        {
            _logger = logger;
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
                                 _watch.Elapsed > ShortDelay && _activeWatch.Elapsed < ShortPeriod;

        public void Wait()
        {
            lock (_lock)
            {
                var lastRequest = _watch.Elapsed;
                if (_watch.Elapsed > ResetPeriod)
                    _activeWatch.Restart();
                var thisDelay = _activeWatch.Elapsed > ShortPeriod ? LongDelay : ShortDelay;
                if (Available)
                {
                    _watch.Restart();
                }
                else
                {
                    _logger.LogTrace($"Time since last command: {lastRequest}, waiting for {thisDelay - lastRequest}");
                    Thread.Sleep(thisDelay - lastRequest);
                    _logger.LogTrace("Sending next command");
                    _watch.Restart();
                }
            }
        }
    }
}
