using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Results;
using Shizou.CommandProcessors;

namespace Shizou.AniDbApi.Requests;

public sealed class EpisodeRequest : AniDbUdpRequest
{
    private EpisodeRequest(IServiceProvider provider) : base(provider.GetRequiredService<ILogger<EpisodeRequest>>(),
        provider.GetRequiredService<AniDbUdp>(),
        provider.GetRequiredService<AniDbUdpProcessor>())
    {
    }

    public EpisodeRequest(IServiceProvider provider, int episodeId) : this(provider)
    {
        Params["eid"] = episodeId.ToString();
    }

    // TODO: Test if epno can take special episode string
    public EpisodeRequest(IServiceProvider provider, int animeId, string episodeNumber) : this(provider)
    {
        Params["aid"] = animeId.ToString();
        Params["epno"] = episodeNumber;
    }

    public override string Command { get; } = "EPISODE";
    public override Dictionary<string, string> Params { get; } = new();

    public AniDbEpisodeResult? EpisodeResult { get; private set; }

    public override async Task Process()
    {
        await SendRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.Episode:
                if (string.IsNullOrWhiteSpace(ResponseText))
                    Errored = true;
                else
                    EpisodeResult = new AniDbEpisodeResult(ResponseText);
                break;
            case AniDbResponseCode.NoSuchEpisode:
                break;
        }
    }
}