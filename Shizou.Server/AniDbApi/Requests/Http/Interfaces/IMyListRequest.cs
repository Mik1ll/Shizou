namespace Shizou.Server.AniDbApi.Requests.Http.Interfaces;

public interface IMyListRequest : IHttpRequest
{
    MyListResult? MyListResult { get; }
}