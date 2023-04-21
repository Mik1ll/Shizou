using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Exceptions;
using Shizou.Options;

namespace Shizou.AniDbApi.Requests.Http;

public abstract class HttpRequest
{
    private readonly HttpClient _httpClient;
    private readonly UriBuilder _builder;
    protected readonly ILogger<HttpRequest> Logger;
    private readonly AniDbHttpState _httpState;

    public Dictionary<string, string?> Args { get; } = new();
    public string? ResponseText { get; protected set; }

    public HttpRequest(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ShizouOptions>>().Value;
        _httpState = provider.GetRequiredService<AniDbHttpState>();
        _httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("gzip");
        _builder = new UriBuilder("http", options.AniDb.ServerHost, options.AniDb.HttpServerPort, "httpapi");
        Logger = provider.GetRequiredService<ILogger<HttpRequest>>();
        Args["client"] = "shizouhttp";
        Args["clientver"] = "1";
        Args["protover"] = "1";
        Args["user"] = options.AniDb.Username;
        Args["pass"] = options.AniDb.Password;
    }

    public abstract Task Process();

    public async Task SendRequest()
    {
        var url = QueryHelpers.AddQueryString(_builder.Uri.AbsoluteUri, Args);
        try
        {
            await _httpState.RateLimiter.EnsureRate();
            if (_httpState.Banned)
            {
                Logger.LogWarning("Banned, aborting HTTP request: {url}", url);
                return;
            }
            Logger.LogInformation("Sending HTTP request: {url}", url);
            ResponseText = await _httpClient.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(ResponseText))
            {
                Logger.LogWarning("No http response, may be banned");
                throw new ProcessorPauseException("No http response, may be banned");
            }
            else if (ResponseText.StartsWith("<error"))
            {
                if (ResponseText.Contains("Banned"))
                {
                    _httpState.Banned = true;
                    Logger.LogWarning("HTTP Banned! waiting {banPeriod}", _httpState.BanPeriod);
                    throw new ProcessorPauseException($"HTTP Banned, wait {_httpState.BanPeriod}");
                }
                else
                {
                    Logger.LogError("Unknown error http response: {errText}", ResponseText);
                    throw new ProcessorPauseException("Unknown error http response, check log");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning("Http request failed: {Message}", ex.Message);
        }
    }
}
