using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi
{
    public sealed class PingRequest : AniDbUdpRequest
    {
        public PingRequest(ILogger<AniDbUdpRequest> logger, AniDbUdp udpApi) : base(logger, udpApi)
        {
        }

        public override string Command { get; } = "PING";
        public override List<(string name, string value)> Params { get; } = new() {("nat", "1")};

        public override async Task Process()
        {
            await SendRequest();
        }
    }
}
