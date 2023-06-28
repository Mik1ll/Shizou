﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.Exceptions;
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

    public HttpRequest(
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

    public abstract Task Process();

    public async Task SendRequest()
    {
        var url = QueryHelpers.AddQueryString(_builder.Uri.AbsoluteUri, Args);
        try
        {
            await _httpState.RateLimiter.EnsureRate();
            if (_httpState.Banned)
            {
                Logger.LogWarning("Banned, aborting HTTP request: {Url}", url);
                return;
            }
            Logger.LogInformation("Sending HTTP request: {Url}", url);
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
                    Logger.LogWarning("HTTP Banned! waiting {BanPeriod}", _httpState.BanPeriod);
                    throw new ProcessorPauseException($"HTTP Banned, wait {_httpState.BanPeriod}");
                }
                else
                {
                    Logger.LogError("Unknown error http response: {ErrText}", ResponseText);
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