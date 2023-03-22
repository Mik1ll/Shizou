using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Exceptions;
using Shizou.Options;

namespace Shizou.AniDbApi.Requests.Udp;

public class AuthRequest : AniDbUdpRequest
{
    public AuthRequest(IServiceProvider provider) : base(provider, "AUTH")
    {
        var opts = provider.GetRequiredService<IOptionsSnapshot<ShizouOptions>>().Value;
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
