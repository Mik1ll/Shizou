using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp.Notify;

public class MessageAckRequest : AniDbUdpRequest
{
    public bool? Success { get; set; }

    public MessageAckRequest(ILogger<MessageAckRequest> logger,
        AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("NOTIFYACK", logger, aniDbUdpState, rateLimiter)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.MessageAckSuccess:
                Success = true;
                break;
            case AniDbResponseCode.NoSuchMessageAck:
                Success = false;
                break;
        }
        return Task.CompletedTask;
    }
}
