using System;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.RateLimiters;

public sealed class HttpRateLimiter : RateLimiter
{
    public HttpRateLimiter(ILogger<HttpRateLimiter> logger) : base(logger)
    {
    }

    protected override TimeSpan ShortDelay { get; } = new(0, 0, 3);
    protected override TimeSpan LongDelay { get; } = new(0, 0, 5);
    protected override TimeSpan ShortPeriod { get; } = new(0, 30, 0);
    protected override TimeSpan ResetPeriod { get; } = new(0, 30, 0);
}