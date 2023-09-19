﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Http;

public abstract class HttpRequest : IHttpRequest
{
    private readonly UriBuilder _builder;
    private readonly HttpClient _httpClient;
    private readonly AniDbHttpState _httpState;
    private readonly ILogger<HttpRequest> _logger;
    private readonly HttpRateLimiter _rateLimiter;

    protected HttpRequest(ILogger<HttpRequest> logger,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        AniDbHttpState httpState,
        IHttpClientFactory httpClientFactory, HttpRateLimiter rateLimiter)
    {
        var options = optionsSnapshot.Value;
        _logger = logger;
        _httpState = httpState;
        _rateLimiter = rateLimiter;
        _httpClient = httpClientFactory.CreateClient("gzip");
        _builder = new UriBuilder("http", options.AniDb.ServerHost, options.AniDb.HttpServerPort, "httpapi");
        Args["client"] = "shizouhttp";
        Args["clientver"] = "1";
        Args["protover"] = "1";
        Args["user"] = options.AniDb.Username;
        Args["pass"] = options.AniDb.Password;
    }

    protected Dictionary<string, string?> Args { get; } = new();
    protected bool ParametersSet { get; set; }
    public string? ResponseText { get; private set; }

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    public async Task Process()
    {
        if (!ParametersSet)
            throw new ArgumentException($"Parameters not set before {nameof(Process)} called");
        await SendRequest();
        await HandleResponse();
    }

    protected abstract Task HandleResponse();

    /// <exception cref="HttpRequestException"></exception>
    private async Task SendRequest()
    {
        var url = QueryHelpers.AddQueryString(_builder.Uri.AbsoluteUri, Args);
        using (await _rateLimiter.AcquireAsync())
        {
            if (_httpState.Banned)
            {
                _logger.LogWarning("Banned, aborting HTTP request: {Url}", url);
                return;
            }

            _logger.LogInformation("Sending HTTP request: {Url}", url);
            ResponseText = await _httpClient.GetStringAsync(url);
        }

        if (string.IsNullOrWhiteSpace(ResponseText))
        {
            _logger.LogWarning("No http response, may be banned");
            throw new HttpRequestException("No http response, may be banned");
        }

        if (ResponseText.StartsWith("<error"))
        {
            if (ResponseText.Contains("Banned"))
            {
                _httpState.Banned = true;
                _logger.LogWarning("HTTP Banned! waiting {BanPeriod}", _httpState.BanPeriod);
                throw new HttpRequestException($"HTTP Banned, wait {_httpState.BanPeriod}");
            }

            _logger.LogError("Unknown error http response: {ErrText}", ResponseText);
            throw new HttpRequestException("Unknown error http response, check log");
        }
    }
}