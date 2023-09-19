namespace Shizou.Server.AniDbApi.Requests.Http.Interfaces;

public interface IAnimeRequest : IHttpRequest
{
    AnimeResult? AnimeResult { get; }
}