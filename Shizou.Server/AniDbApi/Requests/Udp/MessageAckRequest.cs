using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MessageAckRequest : AniDbUdpRequest
{
    public bool? Success { get; set; }

    public MessageAckRequest(
        ILogger<MessageAckRequest> logger,
        AniDbUdpState aniDbUdpState
    ) : base("NOTIFYACK", logger, aniDbUdpState)
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
