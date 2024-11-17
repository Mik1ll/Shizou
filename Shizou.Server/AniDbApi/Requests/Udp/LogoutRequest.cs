using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

// ReSharper disable once ClassNeverInstantiated.Global
public class LogoutRequest : AniDbUdpRequest<UdpResponse>, ILogoutRequest
{
    public LogoutRequest(ILogger<LogoutRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("LOGOUT", logger, aniDbUdpState,
        rateLimiter)
    {
    }

    public void SetParameters()
    {
        ParametersSet = true;
    }

    protected override UdpResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        switch (responseCode)
        {
            case AniDbResponseCode.LoggedOut:
                Logger.LogInformation("Sucessfully logged out of AniDB");
                AniDbUdpState.SessionKey = null;
                break;
            case AniDbResponseCode.NotLoggedIn:
                Logger.LogInformation("Already logged out of AniDB");
                AniDbUdpState.SessionKey = null;
                break;
        }

        return base.CreateResponse(responseText, responseCode, responseCodeText);
    }
}
