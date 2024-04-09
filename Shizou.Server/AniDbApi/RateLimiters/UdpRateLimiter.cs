using System;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.RateLimiters;

public sealed class UdpRateLimiter : RateLimiter
{
    public UdpRateLimiter(ILogger<UdpRateLimiter> logger) : base(logger)
    {
    }

    protected override TimeSpan ShortDelay { get; } = new(0, 0, 0, 2, 500);
    protected override TimeSpan LongDelay { get; } = new(0, 0, 0, 4, 500);
    protected override TimeSpan ShortPeriod { get; } = new(0, 5, 0);
    protected override TimeSpan ResetPeriod { get; } = new(0, 30, 0);
}
