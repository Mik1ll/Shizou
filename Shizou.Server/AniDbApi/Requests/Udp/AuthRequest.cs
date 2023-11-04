using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Udp;

// ReSharper disable once ClassNeverInstantiated.Global
public class AuthRequest : AniDbUdpRequest, IAuthRequest
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
    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.LoginAccepted or AniDbResponseCode.LoginAcceptedNewVersion:
                var split = ResponseCodeString?.Split(" ");
                AniDbUdpState.SessionKey = split?[0];
                // ReSharper disable once UnusedVariable
                var ipEndpoint = split?[1];
                var imageServer = ResponseText?.Trim();
                var opts = _optionsMonitor.CurrentValue;
                if (imageServer is not null && opts.AniDb.ImageServerHost != imageServer)
                {
                    opts.AniDb.ImageServerHost = imageServer;
                    opts.SaveToFile();
                }

                AniDbUdpState.LoggedIn = true;
                AniDbUdpState.ResetLogoutTimer();
                break;
            case AniDbResponseCode.LoginFailed:
                throw new AniDbUdpRequestException("Login failed, change credentials", ResponseCode);
            case AniDbResponseCode.ClientOutdated:
                throw new AniDbUdpRequestException("Login failed, client outdated", ResponseCode);
            case AniDbResponseCode.ClientBanned:
                throw new AniDbUdpRequestException("Login failed, client banned", ResponseCode);
            case null:
                throw new AniDbUdpRequestException("No auth response");
        }

        return Task.CompletedTask;
    }
}
