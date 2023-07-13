using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Http;

public abstract class HttpRequest
{
    private readonly HttpClient _httpClient;
    private readonly UriBuilder _builder;
    protected readonly ILogger<HttpRequest> Logger;
    private readonly AniDbHttpState _httpState;

    public Dictionary<string, string?> Args { get; } = new();
    public bool ParametersSet { get; set; }
    public string? ResponseText { get; protected set; }

    protected HttpRequest(
        ILogger<HttpRequest> logger,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        AniDbHttpState httpState,
        IHttpClientFactory httpClientFactory
    )
    {
        var options = optionsSnapshot.Value;
        Logger = logger;
        _httpState = httpState;
        _httpClient = httpClientFactory.CreateClient("gzip");
        _builder = new UriBuilder("http", options.AniDb.ServerHost, options.AniDb.HttpServerPort, "httpapi");
        Args["client"] = "shizouhttp";
        Args["clientver"] = "1";
        Args["protover"] = "1";
        Args["user"] = options.AniDb.Username;
        Args["pass"] = options.AniDb.Password;
    }

    protected abstract Task HandleResponse();

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    public async Task Process()
    {
        if (!ParametersSet)
            throw new ArgumentException($"Parameters not set before {nameof(Process)} called");
        await SendRequest();
        await HandleResponse();
    }

    /// <exception cref="HttpRequestException"></exception>
    private async Task SendRequest()
    {
        var url = QueryHelpers.AddQueryString(_builder.Uri.AbsoluteUri, Args);
        using (await _httpState.RateLimiter.AcquireAsync())
        {
            if (_httpState.Banned)
            {
                Logger.LogWarning("Banned, aborting HTTP request: {Url}", url);
                return;
            }
            Logger.LogInformation("Sending HTTP request: {Url}", url);
            ResponseText = await _httpClient.GetStringAsync(url);
        }
        if (string.IsNullOrWhiteSpace(ResponseText))
        {
            Logger.LogWarning("No http response, may be banned");
            throw new HttpRequestException("No http response, may be banned");
        }
        if (ResponseText.StartsWith("<error"))
        {
            if (ResponseText.Contains("Banned"))
            {
                _httpState.Banned = true;
                Logger.LogWarning("HTTP Banned! waiting {BanPeriod}", _httpState.BanPeriod);
                throw new HttpRequestException($"HTTP Banned, wait {_httpState.BanPeriod}");
            }
            else
            {
                Logger.LogError("Unknown error http response: {ErrText}", ResponseText);
                throw new HttpRequestException("Unknown error http response, check log");
            }
        }
    }
}
