using System;
using Microsoft.Extensions.DependencyInjection;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Http;

public class HttpRequestFactory
{
    private readonly IServiceProvider _provider;

    public HttpRequestFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IAnimeRequest AnimeRequest(int aid)
    {
        var request = _provider.GetRequiredService<IAnimeRequest>();
        request.Args["request"] = "anime";
        request.Args["aid"] = aid.ToString();
        request.ParametersSet = true;
        return request;
    }

    public IMyListRequest MyListRequest()
    {
        var request = _provider.GetRequiredService<IMyListRequest>();
        request.Args["request"] = "mylist";
        request.ParametersSet = true;
        return request;
    }
}
