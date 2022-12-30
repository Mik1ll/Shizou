using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.Requests;

public class LogoutRequest : AniDbUdpRequest
{
    public LogoutRequest(IServiceProvider provider) : base(provider)
    {
    }

    public override string Command { get; } = "LOGOUT";
    public override Dictionary<string, string> Params { get; } = new();

    public override async Task Process()
    {
        Logger.LogInformation("Attempting to log out of AniDB");
        await SendRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.LoggedOut:
                Logger.LogInformation("Sucessfully logged out of AniDB");
                AniDbUdp.SessionKey = null;
                AniDbUdp.LoggedIn = false;
                break;
            case AniDbResponseCode.NotLoggedIn:
                Logger.LogInformation("Already logged out of AniDB");
                AniDbUdp.SessionKey = null;
                AniDbUdp.LoggedIn = false;
                break;
        }
    }
}