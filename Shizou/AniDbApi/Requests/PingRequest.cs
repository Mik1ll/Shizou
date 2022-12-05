using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;

namespace Shizou.AniDbApi.Requests;

public sealed class PingRequest : AniDbUdpRequest
{
    public PingRequest(IServiceProvider provider) : base(
        provider.GetRequiredService<ILogger<PingRequest>>(),
        provider.GetRequiredService<AniDbUdp>(),
        provider.GetRequiredService<AniDbUdpProcessor>())
    {
    }

    public override string Command { get; } = "PING";
    public override Dictionary<string, string> Params { get; } = new() { { "nat", "1" } };

    public override async Task Process()
    {
        Logger.LogDebug("Pinging server...");
        await SendRequest();
        Logger.LogDebug("Ping Response: {responseCode}", ResponseCode);
    }
}