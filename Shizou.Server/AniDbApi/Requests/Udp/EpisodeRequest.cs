using System;
using System.Threading.Tasks;
using Shizou.Server.AniDbApi.Requests.Udp.Results;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class EpisodeRequest : AniDbUdpRequest
{
    private EpisodeRequest(IServiceProvider provider) : base(provider, "EPISODE")
    {
    }

    public EpisodeRequest(IServiceProvider provider, int episodeId) : this(provider)
    {
        Args["eid"] = episodeId.ToString();
    }

    // TODO: Test if epno can take special episode string
    public EpisodeRequest(IServiceProvider provider, int animeId, string episodeNumber) : this(provider)
    {
        Args["aid"] = animeId.ToString();
        Args["epno"] = episodeNumber;
    }
    
    public AniDbEpisodeResult? EpisodeResult { get; private set; }

    public override async Task Process()
    {
        await HandleRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.Episode:
                if (!string.IsNullOrWhiteSpace(ResponseText))
                    EpisodeResult = new AniDbEpisodeResult(ResponseText);
                break;
            case AniDbResponseCode.NoSuchEpisode:
                break;
        }
    }
}