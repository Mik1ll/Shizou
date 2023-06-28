using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class PingRequest : AniDbUdpRequest
{
    public PingRequest(ILogger<PingRequest> logger, AniDbUdpState aniDbUdpState) : base("PING", logger, aniDbUdpState)
    {
        Args["nat"] = "1";
    }

    protected override Task HandleResponse()
    {
        Logger.LogDebug("Ping Response: {ResponseCode}", ResponseCode);
        return Task.CompletedTask;
    }
}