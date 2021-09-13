using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.Requests
{
    public class LogoutRequest : AniDbUdpRequest
    {
        public LogoutRequest(IServiceProvider provider) : base(provider.GetRequiredService<ILogger<LogoutRequest>>(), provider.GetRequiredService<AniDbUdp>())
        {
        }

        public override string Command { get; } = "LOGOUT";
        public override Dictionary<string, string> Params { get; } = new();

        public override async Task Process()
        {
            Logger.LogInformation("Attempting to log out of AniDB");
            await SendRequest();

            if (ResponseCode is AniDbResponseCode.LoggedOut or AniDbResponseCode.NotLoggedIn)
            {
                AniDbUdp.SessionKey = null;
                AniDbUdp.LoggedIn = false;
            }
        }
    }
}
