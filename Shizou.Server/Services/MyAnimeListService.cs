using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data.Database;
using Shizou.Data.Enums.Mal;
using Shizou.Data.Models;
using Shizou.Data.Utilities;
using Shizou.Server.Options;
using Base64UrlTextEncoder = Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder;

namespace Shizou.Server.Services;

public class MyAnimeListService
{
    private static string? _codeChallengeAndVerifier;
    private static string? _state;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MyAnimeListService> _logger;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

    public MyAnimeListService(
        ILogger<MyAnimeListService> logger,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        IDbContextFactory<ShizouContext> contextFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _contextFactory = contextFactory;
    }

    public string? GetAuthenticationUrl(string serverAccessIp)
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

        var url = QueryHelpers.AddQueryString("https://myanimelist.net/v1/oauth2/authorize", new Dictionary<string, string?>
        {
            { "response_type", "code" },
            { "client_id", options.ClientId },
            { "state", _state },
            { "code_challenge", _codeChallengeAndVerifier },
            { "code_challenge_method", "plain" }
        });
        _logger.LogInformation("Created MAL auth flow url {Url}", url);
        return url;
    }

    public async Task<bool> GetToken(string code, string state)
    {
        _logger.LogInformation("Got auth code, requesting new tokens");
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
        _logger.LogInformation("Refreshing MAL auth tokens");
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

    public async Task GetUserAnimeList()
    {
        var options = _optionsMonitor.CurrentValue;
        if (options.MyAnimeList.MyAnimeListToken is null)
        {
            _logger.LogWarning("MyAnimeList not authenticated, aborting get user list");
            return;
        }

        if (ShouldRefresh(options.MyAnimeList.MyAnimeListToken))
            await RefreshToken();

        var animeWithStatus = new HashSet<int>();

        // ReSharper disable once MethodHasAsyncOverload
        var context = _contextFactory.CreateDbContext();
        var malAnimes = context.MalAnimes.ToDictionary(a => a.Id);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.MyAnimeList.MyAnimeListToken.AccessToken);
        var url = QueryHelpers.AddQueryString("https://api.myanimelist.net/v2/users/@me/animelist", new Dictionary<string, string?>
        {
            { "nsfw", "true" },
            { "fields", "list_status{status,num_episodes_watched,updated_at},node{id,title,media_type,num_episodes}" },
            { "limit", "1000" }
        });
        while (url is not null)
        {
            var result = await httpClient.GetAsync(url);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError("Something went wrong when getting user list, returned status {StatusCode}", result.StatusCode);
                return;
            }

            try
            {
                using var doc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync());
                var root = doc.RootElement;
                var nextPage = root.GetProperty("paging").TryGetProperty("next", out var nextPageElem) ? nextPageElem.GetString() : null;
                var data = root.GetProperty("data");
                foreach (var item in data.EnumerateArray())
                {
                    var anime = item.GetProperty("node");
                    var id = anime.GetProperty("id").GetInt32();
                    animeWithStatus.Add(id);
                    var title = anime.GetProperty("title").GetString()!;
                    // ReSharper disable once MethodHasAsyncOverload

                    if (!malAnimes.TryGetValue(id, out var dbAnime))
                        dbAnime = context.MalAnimes.Add(new MalAnime { Id = id, Title = title }).Entity;
                    else
                        dbAnime.Title = title;

                    var listStatus = item.GetProperty("list_status");
                    var state = listStatus.GetProperty("status").GetString()!;
                    var stateEnum = Enum.Parse<AnimeState>(state.Replace("_", string.Empty), true);
                    var watched = listStatus.GetProperty("num_episodes_watched").GetInt32();
                    var updated = listStatus.GetProperty("updated_at").GetDateTimeOffset();
                    if (dbAnime.Status is null)
                    {
                        dbAnime.Status = new MalStatus { State = stateEnum, Updated = updated.UtcDateTime, WatchedEpisodes = watched };
                    }
                    else
                    {
                        dbAnime.Status.State = stateEnum;
                        dbAnime.Status.Updated = updated.UtcDateTime;
                        dbAnime.Status.WatchedEpisodes = watched;
                    }
                }

                url = nextPage;
            }
            catch (JsonException)
            {
                _logger.LogError("Failed to parse json from get user list, aborting");
                return;
            }
        }

        foreach (var anime in malAnimes.Values.ExceptBy(animeWithStatus, x => x.Id))
            anime.Status = null;

        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges();
    }

    private static bool ShouldRefresh(MyAnimeListToken token)
    {
        return token.Expiration <= DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
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