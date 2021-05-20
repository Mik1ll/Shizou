using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.AniDbApi
{
    public sealed class AuthRequest : AniDbUdpRequest
    {
        public AuthRequest(ILogger<AniDbUdpRequest> logger, AniDbUdp udpApi, IOptionsMonitor<ShizouOptions> options) : base(logger, udpApi)
        {
            var opts = options.CurrentValue;
            Params.Add(("user", opts.AniDb.Username));
            Params.Add(("pass", opts.AniDb.Password));
            Params.Add(("enc", Encoding.BodyName));
        }

        public override string Command { get; } = "AUTH";

        public override List<(string name, string value)> Params { get; } = new()
        {
            ("protover", "3"),
            ("client", "Shizou"),
            ("clientver", "1"),
            ("comp", "1"),
            ("mtu", "1400"),
            ("imgserver", "1"),
        };

        public override Task Process()
        {
            throw new System.NotImplementedException();
        }
    }
}
