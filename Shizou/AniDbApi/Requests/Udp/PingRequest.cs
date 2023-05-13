using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.Requests.Udp;

public class PingRequest : AniDbUdpRequest
{
    public PingRequest(IServiceProvider provider) : base(provider, "PING")
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