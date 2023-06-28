using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.Requests.Udp.Results;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class EpisodeRequest : AniDbUdpRequest
{
    public EpisodeRequest(ILogger<EpisodeRequest> logger, AniDbUdpState aniDbUdpState) : base("EPISODE", logger, aniDbUdpState)
    {
    }

    public AniDbEpisodeResult? EpisodeResult { get; private set; }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.Episode:
                if (!string.IsNullOrWhiteSpace(ResponseText))
                    EpisodeResult = new AniDbEpisodeResult(ResponseText);
                break;
            case AniDbResponseCode.NoSuchEpisode:
                break;
        }
        return Task.CompletedTask;
    }
}