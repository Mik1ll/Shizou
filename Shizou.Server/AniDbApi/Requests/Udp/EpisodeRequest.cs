using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class EpisodeResponse : UdpResponse
{
    public EpisodeResult? EpisodeResult { get; init; }
}

public class EpisodeRequest : AniDbUdpRequest<EpisodeResponse>, IEpisodeRequest
{
    public EpisodeRequest(ILogger<EpisodeRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("EPISODE", logger, aniDbUdpState,
        rateLimiter)
    {
    }


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

    protected override EpisodeResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        EpisodeResult? episodeResult = null;
        switch (responseCode)
        {
            case AniDbResponseCode.Episode:
                if (!string.IsNullOrWhiteSpace(responseText))
                    episodeResult = new EpisodeResult(responseText);
                break;
            case AniDbResponseCode.NoSuchEpisode:
                break;
        }

        return new EpisodeResponse
        {
            ResponseText = responseText,
            ResponseCode = responseCode,
            ResponseCodeText = responseCodeText,
            EpisodeResult = episodeResult
        };
    }
    
}
