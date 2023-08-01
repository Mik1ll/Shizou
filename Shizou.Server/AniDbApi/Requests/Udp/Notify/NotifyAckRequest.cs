using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp.Notify;

public class NotifyAckRequest : AniDbUdpRequest
{
    public bool? Success { get; set; }

    public NotifyAckRequest(ILogger<NotifyAckRequest> logger,
        AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("NOTIFYACK", logger, aniDbUdpState, rateLimiter)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.NotifyAckSuccess:
                Success = true;
                break;
            case AniDbResponseCode.NoSuchNotifyAck:
                Success = false;
                break;
        }
        return Task.CompletedTask;
    }
}
