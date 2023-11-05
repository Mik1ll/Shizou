namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IAnimeRequest : IAniDbUdpRequest<AnimeResponse>
{
    void SetParameters(int aid, AMaskAnime aMask);
}
