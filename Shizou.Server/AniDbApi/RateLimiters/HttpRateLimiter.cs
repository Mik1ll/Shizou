using System;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.RateLimiters;

public sealed class HttpRateLimiter : RateLimiter
{
    public HttpRateLimiter(ILogger<HttpRateLimiter> logger) : base(logger)
    {
    }

    protected override TimeSpan ShortDelay { get; } = new(0, 0, 5);
    protected override TimeSpan LongDelay { get; } = new(0, 0, 5);
    protected override TimeSpan ShortPeriod { get; } = TimeSpan.MaxValue;
    protected override TimeSpan ResetPeriod { get; } = TimeSpan.MaxValue;
}
