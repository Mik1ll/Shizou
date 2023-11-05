namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IEpisodeRequest : IAniDbUdpRequest<EpisodeResponse>
{
    void SetParameters(int episodeId);
    void SetParameters(int animeId, string episodeNumber);
}
