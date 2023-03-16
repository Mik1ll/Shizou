using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.AniDbApi.Requests.Udp;

public class AuthRequest : AniDbUdpRequest
{
    public AuthRequest(IServiceProvider provider) : base(provider, "AUTH")
    {
        var opts = provider.GetRequiredService<IOptionsSnapshot<ShizouOptions>>().Value;
        Params["user"] = opts.AniDb.Username;
        Params["pass"] = opts.AniDb.Password;
        Params["protover"] = "3";
        Params["client"] = "shizouudp";
        Params["clientver"] = "1";
        Params["comp"] = "1";
        Params["enc"] = Encoding.BodyName;
        Params["mtu"] = "1400";
        Params["imgserver"] = "1";
        Params["nat"] = "1";
    }

    public override async Task Process()
    {
        Logger.LogInformation("Attempting to log into AniDB");
        await SendRequest();
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
                Errored = true;
                UdpProcessor.Pause("Login failed, change credentials");
                break;
            case AniDbResponseCode.ClientOutdated:
                Errored = true;
                UdpProcessor.Pause("Login failed, client outdated");
                break;
            case AniDbResponseCode.ClientBanned:
                Errored = true;
                UdpProcessor.Pause("Login failed, client banned");
                break;
            case null:
                Errored = true;
                UdpProcessor.Pause("No auth response");
                break;
        }
    }
}
