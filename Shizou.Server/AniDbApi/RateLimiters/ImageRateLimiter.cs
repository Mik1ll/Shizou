using System;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.RateLimiters;

public class ImageRateLimiter : RateLimiter
{
    public ImageRateLimiter(ILogger<ImageRateLimiter> logger) : base(logger)
    {
    }

    protected override TimeSpan ShortDelay { get; } = TimeSpan.FromMilliseconds(200);
    protected override TimeSpan LongDelay { get; } = TimeSpan.FromMilliseconds(200);
    protected override TimeSpan ShortPeriod { get; } = TimeSpan.MaxValue;
    protected override TimeSpan ResetPeriod { get; } = TimeSpan.MaxValue;
}
