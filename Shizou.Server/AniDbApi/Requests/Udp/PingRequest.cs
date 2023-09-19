using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class PingRequest : AniDbUdpRequest, IPingRequest
{
    public PingRequest(ILogger<PingRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("PING", logger, aniDbUdpState, rateLimiter)
    {
        Args["nat"] = "1";
    }

    public void SetParameters()
    {
        ParametersSet = true;
    }

    protected override Task HandleResponse()
    {
        Logger.LogDebug("Ping Response: {ResponseCode}", ResponseCode);
        return Task.CompletedTask;
    }
}