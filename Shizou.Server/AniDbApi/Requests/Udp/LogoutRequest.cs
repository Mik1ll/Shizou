using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class LogoutRequest : AniDbUdpRequest
{
    public LogoutRequest(ILogger<LogoutRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("LOGOUT", logger, aniDbUdpState,
        rateLimiter)
    {
    }

    protected override Task HandleResponse()
    {
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
        return Task.CompletedTask;
    }
}
