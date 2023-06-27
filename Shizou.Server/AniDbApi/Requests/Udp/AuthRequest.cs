using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.Exceptions;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class AuthRequest : AniDbUdpRequest
{
    public AuthRequest(
        ILogger<AuthRequest> logger,
        AniDbUdpState aniDbUdpState
    ) : base("AUTH", logger, aniDbUdpState)
    {
    }

    public override async Task Process()
    {
        Logger.LogInformation("Attempting to log into AniDB");
        await HandleRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.LoginAccepted or AniDbResponseCode.LoginAcceptedNewVersion:
                var split = ResponseCodeString?.Split(" ");
                AniDbUdpState.SessionKey = split?[0];
                // ReSharper disable once UnusedVariable
                var ipEndpoint = split?[1];
                AniDbUdpState.ImageServerUrl = ResponseText?.Trim();
                AniDbUdpState.LoggedIn = true;
                break;
            case AniDbResponseCode.LoginFailed:
                throw new ProcessorPauseException("Login failed, change credentials");
            case AniDbResponseCode.ClientOutdated:
                throw new ProcessorPauseException("Login failed, client outdated");
            case AniDbResponseCode.ClientBanned:
                throw new ProcessorPauseException("Login failed, client banned");
            case null:
                throw new ProcessorPauseException("No auth response");
        }
    }
}
