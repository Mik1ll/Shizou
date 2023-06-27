using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class PingRequest : AniDbUdpRequest
{
    public PingRequest(ILogger<PingRequest> logger, AniDbUdpState aniDbUdpState) : base("PING", logger, aniDbUdpState)
    {
        Args["nat"] = "1";
    }
    
    public override async Task Process()
    {
        Logger.LogDebug("Pinging server...");
        await HandleRequest();
        Logger.LogDebug("Ping Response: {ResponseCode}", ResponseCode);
    }
}