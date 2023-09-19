namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IAnimeRequest : IAniDbUdpRequest
{
    AnimeResult? AnimeResult { get; }
    void SetParameters(int aid, AMaskAnime aMask);
}