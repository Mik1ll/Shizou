using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class EpisodeRequest : AniDbUdpRequest, IEpisodeRequest
{
    public EpisodeRequest(ILogger<EpisodeRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("EPISODE", logger, aniDbUdpState,
        rateLimiter)
    {
    }

    public EpisodeResult? EpisodeResult { get; private set; }

    public void SetParameters(int episodeId)
    {
        Args["eid"] = episodeId.ToString();
        ParametersSet = true;
    }

    // TODO: Test if epno can take special episode string
    public void SetParameters(int animeId, string episodeNumber)
    {
        Args["aid"] = animeId.ToString();
        Args["epno"] = episodeNumber;
        ParametersSet = true;
    }
    
    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.Episode:
                if (!string.IsNullOrWhiteSpace(ResponseText))
                    EpisodeResult = new EpisodeResult(ResponseText);
                break;
            case AniDbResponseCode.NoSuchEpisode:
                break;
        }
        return Task.CompletedTask;
    }
}