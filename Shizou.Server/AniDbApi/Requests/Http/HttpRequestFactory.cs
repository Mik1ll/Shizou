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
        return ActivatorUtilities.CreateInstance<AnimeRequest>(_provider, aid);
    }

    public MyListRequest MyListRequest()
    {
        return ActivatorUtilities.CreateInstance<MyListRequest>(_provider);
    }
}
