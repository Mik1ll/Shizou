using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp.Notify;

public class NotifyAckRequest : AniDbUdpRequest
{
    public bool? Success { get; set; }

    public NotifyAckRequest(
        ILogger<NotifyAckRequest> logger,
        AniDbUdpState aniDbUdpState
    ) : base("NOTIFYACK", logger, aniDbUdpState)
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
