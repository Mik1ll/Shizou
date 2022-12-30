using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shizou.AniDbApi.Results;

namespace Shizou.AniDbApi.Requests;

public sealed class EpisodeRequest : AniDbUdpRequest
{
    private EpisodeRequest(IServiceProvider provider) : base(provider)
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