using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shizou.Server.AniDbApi.Requests.Http;

public class HttpRequestFactory
{
    private readonly IServiceProvider _provider;

    public HttpRequestFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public AnimeRequest AnimeRequest(int aid)
    {
        var request = _provider.GetRequiredService<AnimeRequest>();
        request.Args["request"] = "anime";
        request.Args["aid"] = aid.ToString();
        request.ParametersSet = true;
        return request;
    }

    public MyListRequest MyListRequest()
    {
        var request = _provider.GetRequiredService<MyListRequest>();
        request.Args["request"] = "mylist";
        request.ParametersSet = true;
        return request;
    }
}
