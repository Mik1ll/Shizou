using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Udp;

// ReSharper disable once ClassNeverInstantiated.Global
public class AuthRequest : AniDbUdpRequest<UdpResponse>, IAuthRequest
{
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;

    public AuthRequest(
        ILogger<AuthRequest> logger,
        AniDbUdpState aniDbUdpState,
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        UdpRateLimiter rateLimiter
    ) : base("AUTH", logger, aniDbUdpState, rateLimiter)
    {
        _optionsMonitor = optionsMonitor;
    }

    public void SetParameters()
    {
        var opts = _optionsMonitor.CurrentValue;
        Args["user"] = opts.AniDb.Username;
        Args["pass"] = opts.AniDb.Password;
        Args["protover"] = "3";
        Args["client"] = "shizouudp";
        Args["clientver"] = "1";
        Args["comp"] = "1";
        Args["enc"] = Encoding.BodyName;
        Args["mtu"] = "1400";
        Args["imgserver"] = "1";
        Args["nat"] = "1";
        ParametersSet = true;
    }

    /// <exception cref="AniDbUdpRequestException"></exception>
    protected override UdpResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        switch (responseCode)
        {
            case AniDbResponseCode.LoginAccepted or AniDbResponseCode.LoginAcceptedNewVersion:
                var split = responseCodeText.Split(" ");
                AniDbUdpState.SessionKey = split[0];
                // ReSharper disable once UnusedVariable
                var ipEndpoint = split[1];
                var imageServer = responseText.Trim();
                var opts = _optionsMonitor.CurrentValue;
                if (opts.AniDb.ImageServerHost != imageServer)
                {
                    opts.AniDb.ImageServerHost = imageServer;
                    opts.SaveToFile();
                }

                AniDbUdpState.ResetLogoutTimer();
                Logger.LogInformation("Logged into AniDB");
                break;
            case AniDbResponseCode.LoginFailed:
                AniDbUdpState.SessionKey = null;
                throw new AniDbUdpRequestException("Login failed, change credentials", responseCode);
            case AniDbResponseCode.ClientOutdated:
                AniDbUdpState.SessionKey = null;
                throw new AniDbUdpRequestException("Login failed, client outdated", responseCode);
            case AniDbResponseCode.ClientBanned:
                AniDbUdpState.SessionKey = null;
                throw new AniDbUdpRequestException("Login failed, client banned", responseCode);
        }

        return base.CreateResponse(responseText, responseCode, responseCodeText);
    }
}
