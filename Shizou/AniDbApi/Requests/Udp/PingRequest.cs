using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.Requests.Udp;

public class PingRequest : AniDbUdpRequest
{
    public PingRequest(IServiceProvider provider) : base(provider, "PING")
    {
        Params["nat"] = "1";
    }
    
    public override async Task Process()
    {
        Logger.LogDebug("Pinging server...");
        await SendRequest();
        Logger.LogDebug("Ping Response: {responseCode}", ResponseCode);
    }
}