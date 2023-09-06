using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Utilities;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class MyAnimeListService
{
    private readonly ILogger<MyAnimeListService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;

    private static string? _codeChallengeAndVerifier;
    private static string? _state;

    public MyAnimeListService(ILogger<MyAnimeListService> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<ShizouOptions> optionsMonitor)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
    }

    public Uri? GetAuthenticationUri(string serverAccessIp)
    {
        var options = _optionsMonitor.CurrentValue.MyAnimeList;
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            _logger.LogError("Tried to authenticate with MAL without a client ID");
            return null;
        }

        if (!NetworkUtility.IsLoopBackAddress(serverAccessIp))
        {
            _logger.LogError("Can only authenticate on localhost due to MAL auth redirect registration");
            return null;
        }

        _codeChallengeAndVerifier = GetCodeVerifier();
        _state = Guid.NewGuid().ToString();

        var authorizeUriBuilder = new UriBuilder("https://myanimelist.net/v1/oauth2/authorize");
        var query = HttpUtility.ParseQueryString(authorizeUriBuilder.Query);
        query["response_type"] = "code";
        query["client_id"] = options.ClientId;
        query["state"] = _state;
        query["code_challenge"] = _codeChallengeAndVerifier;
        query["code_challenge_method"] = "plain";
        authorizeUriBuilder.Query = query.ToString();
        return authorizeUriBuilder.Uri;
    }

    public async Task<bool> GetToken(string code, string state)
    {
        var options = _optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.MyAnimeList.ClientId))
        {
            _logger.LogError("Tried to authenticate with MAL without a client ID");
            return false;
        }

        if (_codeChallengeAndVerifier is null)
        {
            _logger.LogError("Tried to get MAL token without a code verifier");
            return false;
        }

        if (_state is null)
        {
            _logger.LogError("Tried to get MAL token without a state");
            return false;
        }

        if (state != _state)
        {
            _logger.LogError("State did not match stored value");
            return false;
        }

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://myanimelist.net/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("client_id", options.MyAnimeList.ClientId),
                new("grant_type", "authorization_code"),
                new("code", code),
                new("code_verifier", _codeChallengeAndVerifier)
            })
        };
        var result = await httpClient.SendAsync(request);
        if (!result.IsSuccessStatusCode)
        {
            _logger.LogError("Something went wrong when getting token, returned status {StatusCode}", result.StatusCode);
            return false;
        }

        var token = await result.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null)
        {
            _logger.LogError("Couldn't get token from response body");
            return false;
        }

        var newToken = new MyAnimeListToken(token.access_token, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(token.expires_in), token.refresh_token);
        options.MyAnimeList.MyAnimeListToken = newToken;
        options.SaveToFile();

        _state = null;
        _codeChallengeAndVerifier = null;
        return true;
    }

    public async Task<bool> RefreshToken()
    {
        var options = _optionsMonitor.CurrentValue;
        if (options.MyAnimeList.MyAnimeListToken is null)
        {
            _logger.LogError("No token to refresh");
            return false;
        }

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://myanimelist.net/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("client_id", options.MyAnimeList.ClientId),
                new("grant_type", "refresh_token"),
                new("refresh_token", options.MyAnimeList.MyAnimeListToken.RefreshToken)
            })
        };

        var result = await httpClient.SendAsync(request);
        if (!result.IsSuccessStatusCode)
        {
            _logger.LogError("Something went wrong when refreshing token, returned status {StatusCode}", result.StatusCode);
            return false;
        }

        var token = await result.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null)
        {
            _logger.LogError("Couldn't get token from response body");
            return false;
        }

        var newToken = new MyAnimeListToken(token.access_token, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(token.expires_in), token.refresh_token);
        options.MyAnimeList.MyAnimeListToken = newToken;
        options.SaveToFile();

        return true;
    }

    private static string GetCodeVerifier()
    {
        return Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    private record TokenResponse(string token_type, int expires_in, string access_token, string refresh_token);
}