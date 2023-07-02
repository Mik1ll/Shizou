using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.Exceptions;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class AuthRequest : AniDbUdpRequest
{
    private readonly IOptionsSnapshot<ShizouOptions> _optionsSnapshot;

    public AuthRequest(
        ILogger<AuthRequest> logger,
        AniDbUdpState aniDbUdpState,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot
    ) : base("AUTH", logger, aniDbUdpState)
    {
        _optionsSnapshot = optionsSnapshot;
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.LoginAccepted or AniDbResponseCode.LoginAcceptedNewVersion:
                var split = ResponseCodeString?.Split(" ");
                AniDbUdpState.SessionKey = split?[0];
                // ReSharper disable once UnusedVariable
                var ipEndpoint = split?[1];
                var options = _optionsSnapshot.Value;
                options.AniDb.ImageServerHost = ResponseText?.Trim();
                options.SaveToFile();
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
        return Task.CompletedTask;
    }
}
