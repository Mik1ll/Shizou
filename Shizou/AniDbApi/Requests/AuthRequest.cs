using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.AniDbApi.Requests;

public sealed class AuthRequest : AniDbUdpRequest
{
    public AuthRequest(IServiceProvider provider) : base(provider)
    {
        var opts = provider.GetRequiredService<IOptionsMonitor<ShizouOptions>>().CurrentValue;
        Params["user"] = opts.AniDb.Username;
        Params["pass"] = opts.AniDb.Password;
        Params["enc"] = Encoding.BodyName;
    }

    public override string Command { get; } = "AUTH";

    public override Dictionary<string, string> Params { get; } = new()
    {
        { "protover", "3" },
        { "client", "shizouudp" },
        { "clientver", "1" },
        { "comp", "1" },
        { "mtu", "1400" },
        { "imgserver", "1" },
        { "nat", "1" }
    };

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