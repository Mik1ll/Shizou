using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi
{
    public class LogoutRequest : AniDbUdpRequest
    {
        public LogoutRequest(ILogger<AniDbUdpRequest> logger, AniDbUdp aniDbUdp) : base(logger, aniDbUdp)
        {
        }

        public override string Command { get; } = "LOGOUT";
        public override List<(string name, string value)> Params { get; } = new();

        public override async Task Process()
        {
            await SendRequest();

            if (ResponseCode is AniDbResponseCode.LoggedOut or AniDbResponseCode.NotLoggedIn)
            {
                AniDbUdp.SessionKey = null;
                AniDbUdp.LoggedIn = false;
            }
        }
    }
}
