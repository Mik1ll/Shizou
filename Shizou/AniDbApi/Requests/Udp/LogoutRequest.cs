using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.Requests.Udp;

public class LogoutRequest : AniDbUdpRequest
{
    public LogoutRequest(IServiceProvider provider) : base(provider, "LOGOUT")
    {
    }
    
    public override async Task Process()
    {
        Logger.LogInformation("Attempting to log out of AniDB");
        await HandleRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.LoggedOut:
                Logger.LogInformation("Sucessfully logged out of AniDB");
                AniDbUdpState.SessionKey = null;
                AniDbUdpState.LoggedIn = false;
                break;
            case AniDbResponseCode.NotLoggedIn:
                Logger.LogInformation("Already logged out of AniDB");
                AniDbUdpState.SessionKey = null;
                AniDbUdpState.LoggedIn = false;
                break;
        }
    }
}