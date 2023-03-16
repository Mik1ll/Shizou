using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.AniDbApi.Requests.Http;

public class HttpRequest
{
    private readonly HttpClient _httpClient;
    private readonly UriBuilder _builder;

    public string? Result { get; set; }
    public Dictionary<string, string?> Params { get; } = new();

    public HttpRequest(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<ShizouOptions>>();
        _httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("gzip");
        _builder = new UriBuilder("http", options.Value.AniDb.ServerHost, options.Value.AniDb.HttpServerPort, "httpapi");
        Params["client"] = "shizouhttp";
        Params["clientver"] = "1";
        Params["protover"] = "1";
        Params["user"] = options.Value.AniDb.Username;
        Params["pass"] = options.Value.AniDb.Password;
    }

    public async void SendRequest()
    {
        var url = QueryHelpers.AddQueryString(_builder.Uri.AbsoluteUri, Params);
        await _httpClient.GetStringAsync(url);
    }
}
