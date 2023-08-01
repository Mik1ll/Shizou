using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class GenericRequest : AniDbUdpRequest
{
    public GenericRequest(ILogger<GenericRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("", logger, aniDbUdpState,
        rateLimiter)
    {
    }

    protected override Task HandleResponse()
    {
        return Task.CompletedTask;
    }
}
