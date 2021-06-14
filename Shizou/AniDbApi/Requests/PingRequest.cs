using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.Requests
{
    public sealed class PingRequest : AniDbUdpRequest
    {
        public PingRequest(ILogger<AniDbUdpRequest> logger, AniDbUdp aniDbUdp) : base(logger, aniDbUdp)
        {
        }

        public override string Command { get; } = "PING";
        public override List<(string name, string value)> Params { get; } = new() {("nat", "1")};

        public override async Task Process()
        {
            Logger.LogDebug("Pinging server...");
            await SendRequest();
            Logger.LogDebug("Ping Response: {responseCode}", ResponseCode);
        }
    }
}
