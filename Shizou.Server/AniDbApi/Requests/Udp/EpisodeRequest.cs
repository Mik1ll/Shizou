using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class EpisodeRequest : AniDbUdpRequest
{
    public EpisodeRequest(ILogger<EpisodeRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("EPISODE", logger, aniDbUdpState,
        rateLimiter)
    {
    }

    public EpisodeResult? EpisodeResult { get; private set; }

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