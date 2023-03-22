using System;
using System.Timers;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.RateLimiters;

namespace Shizou.AniDbApi;

public class AniDbHttpState : IDisposable
{
    private readonly Timer _bannedTimer;
    private readonly ILogger<AniDbHttpState> _logger;
    private bool _banned;

    public AniDbHttpState(ILogger<AniDbHttpState> logger, HttpRateLimiter limiter)
    {
        _logger = logger;
        RateLimiter = limiter;

        _bannedTimer = new Timer(BanPeriod.TotalMilliseconds);
        _bannedTimer.Elapsed += (_, _) =>
        {
            _logger.LogInformation("Http ban timer has elapsed: {BanPeriod}", BanPeriod);
            Banned = false;
        };
        _bannedTimer.AutoReset = false;
    }

    public HttpRateLimiter RateLimiter { get; }

    public bool Banned
    {
        get => _banned;
        set
        {
            _banned = value;
            if (value)
            {
                _bannedTimer.Stop();
                _bannedTimer.Start();
                BanEndTime = DateTimeOffset.UtcNow + BanPeriod;
            }
            else
            {
                BanEndTime = null;
            }
        }
    }

    public TimeSpan BanPeriod { get; } = new(12, 0, 0);

    public DateTimeOffset? BanEndTime { get; set; }

    public void Dispose()
    {
        _bannedTimer.Dispose();
    }
}
