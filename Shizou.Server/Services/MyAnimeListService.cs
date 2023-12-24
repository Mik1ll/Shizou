using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums.Mal;
using Shizou.Data.Models;
using Shizou.Server.Options;
using Base64UrlTextEncoder = Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder;

namespace Shizou.Server.Services;

public class MyAnimeListService
{
    private readonly IShizouContextFactory _contextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MyAnimeListService> _logger;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private string? _codeChallengeAndVerifier;
    private string? _state;

    public MyAnimeListService(
        ILogger<MyAnimeListService> logger,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        IShizouContextFactory contextFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _contextFactory = contextFactory;
    }

    private static void UpsertAnime(IShizouContext context, MalAnime anime)
    {
        var eAnime = context.MalAnimes.Find(anime.Id);
        if (eAnime is null)
        {
            context.MalAnimes.Add(anime);
        }
        else
        {
            context.Entry(eAnime).CurrentValues.SetValues(anime);
            if (anime.Status is null || eAnime.Status is null)
                eAnime.Status = anime.Status;
            else
                context.Entry(eAnime.Status).CurrentValues.SetValues(anime.Status);
        }
    }

    private static MalStatus StatusFromJson(JsonElement listStatus)
    {
        var state = listStatus.GetProperty("status").GetString()!;
        var stateEnum = Enum.Parse<AnimeState>(state.Replace("_", string.Empty), true);
        var watched = listStatus.GetProperty("num_episodes_watched").GetInt32();
        var updated = listStatus.GetProperty("updated_at").GetDateTimeOffset();
        var status = new MalStatus { State = stateEnum, Updated = updated.UtcDateTime, WatchedEpisodes = watched };
        return status;
    }

    private static MalAnime AnimeFromJson(JsonElement anime)
    {
        var id = anime.GetProperty("id").GetInt32();
        var title = anime.GetProperty("title").GetString()!;
        var type = anime.GetProperty("media_type").GetString()!;
        int? episodeCount = anime.GetProperty("num_episodes").GetInt32() is var num && num > 0 ? num : null;

        var dbAnime = new MalAnime { Id = id, Title = title, AnimeType = type, EpisodeCount = episodeCount };
        return dbAnime;
    }

    private static string GetCodeVerifier() => Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static async Task<MyAnimeListToken?> ReadTokenFromFileAsync()
    {
        try
        {
            var tokenStream = new FileStream(FilePaths.MyAnimeListTokenPath, FileMode.Open, FileAccess.Read);
            MyAnimeListToken? malToken;
            await using (tokenStream.ConfigureAwait(false))
            {
                malToken = await JsonSerializer.DeserializeAsync<MyAnimeListToken>(tokenStream).ConfigureAwait(false);
            }

            return malToken;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private static async Task WriteTokenToFileAsync(MyAnimeListToken newToken)
    {
        var tokenStream = new FileStream(FilePaths.MyAnimeListTokenPath, FileMode.Create, FileAccess.Write);
        await using (tokenStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(tokenStream, newToken).ConfigureAwait(false);
        }
    }

    public string? GetAuthenticationUrl(string serverAccessIp)
    {
        var options = _optionsMonitor.CurrentValue.MyAnimeList;
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            _logger.LogError("Tried to authenticate with MAL without a client ID");
            return null;
        }

        if (!IPAddress.IsLoopback(IPAddress.Parse(serverAccessIp)))
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

    public async Task<bool> GetTokenAsync(string code, string state)
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
        var result = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!HandleStatusCode(result))
            return false;

        if (!await SaveTokenAsync(result).ConfigureAwait(false))
            return false;

        _state = null;
        _codeChallengeAndVerifier = null;
        return true;
    }

    public async Task GetAnimeAsync(int animeId)
    {
        var malToken = await ReadTokenFromFileAsync().ConfigureAwait(false);
        if (malToken is null)
        {
            _logger.LogInformation("MyAnimeList not authenticated, aborting get anime");
            return;
        }

        if (!await RefreshTokenAsync(malToken).ConfigureAwait(false))
            return;

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", malToken.AccessToken);

        var url = QueryHelpers.AddQueryString($"https://api.myanimelist.net/v2/anime/{animeId}", new Dictionary<string, string?>
        {
            { "nsfw", "true" },
            { "fields", "id,title,media_type,num_episodes,my_list_status{status,num_episodes_watched,updated_at}" }
        });

        var result = await httpClient.GetAsync(url).ConfigureAwait(false);
        if (!HandleStatusCode(result))
            return;
        try
        {
            using var doc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
            var animeJson = doc.RootElement;
            var anime = AnimeFromJson(animeJson);

            if (animeJson.TryGetProperty("my_list_status", out var statusJson))
                anime.Status = StatusFromJson(statusJson);

            using var context = _contextFactory.CreateDbContext();
            UpsertAnime(context, anime);

            context.SaveChanges();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse json from get user list, aborting");
        }
    }

    public async Task GetUserAnimeListAsync()
    {
        var malToken = await ReadTokenFromFileAsync().ConfigureAwait(false);
        if (malToken is null)
        {
            _logger.LogWarning("MyAnimeList not authenticated, aborting get user list");
            return;
        }

        if (!await RefreshTokenAsync(malToken).ConfigureAwait(false))
            return;

        var animeWithStatus = new HashSet<int>();

        using var context = _contextFactory.CreateDbContext();
        var malAnimes = context.MalAnimes.ToDictionary(a => a.Id);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", malToken.AccessToken);
        var url = QueryHelpers.AddQueryString("https://api.myanimelist.net/v2/users/@me/animelist", new Dictionary<string, string?>
        {
            { "nsfw", "true" },
            { "fields", "id,title,media_type,num_episodes,list_status{status,num_episodes_watched,updated_at}" },
            { "limit", "1000" }
        });
        while (url is not null)
        {
            var result = await httpClient.GetAsync(url).ConfigureAwait(false);
            if (!HandleStatusCode(result))
                return;

            try
            {
                using var doc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
                var root = doc.RootElement;
                var nextPage = root.GetProperty("paging").TryGetProperty("next", out var nextPageElem) ? nextPageElem.GetString() : null;
                var data = root.GetProperty("data");
                foreach (var item in data.EnumerateArray())
                {
                    var animeJson = item.GetProperty("node");
                    var anime = AnimeFromJson(animeJson);
                    if (!malAnimes.ContainsKey(anime.Id))
                        continue;

                    var statusJson = item.GetProperty("list_status");
                    anime.Status = StatusFromJson(statusJson);

                    animeWithStatus.Add(anime.Id);

                    UpsertAnime(context, anime);
                }

                url = nextPage;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse json from get user list, aborting");
                return;
            }
        }

        foreach (var anime in malAnimes.Values.ExceptBy(animeWithStatus, x => x.Id))
            anime.Status = null;


        context.SaveChanges();
    }

    public async Task<bool> UpdateAnimeStatusAsync(int animeId, MalStatus status)
    {
        var malToken = await ReadTokenFromFileAsync().ConfigureAwait(false);
        if (malToken is null)
        {
            _logger.LogWarning("MyAnimeList not authenticated, aborting update anime state");
            return false;
        }

        if (!await RefreshTokenAsync(malToken).ConfigureAwait(false))
            return false;

        using var context = _contextFactory.CreateDbContext();
        var anime = context.MalAnimes.Find(animeId);
        if (anime is null)
        {
            _logger.LogError("Tried to update status of MAL anime that is not in database");
            return false;
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", malToken.AccessToken);

        var stateStr = status.State switch
        {
            AnimeState.Watching => "watching",
            AnimeState.Completed => "completed",
            AnimeState.OnHold => "on_hold",
            AnimeState.Dropped => "dropped",
            AnimeState.PlanToWatch => "plan_to_watch",
            _ => throw new ArgumentOutOfRangeException(nameof(status.State), status.State, null)
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, QueryHelpers.AddQueryString($"https://api.myanimelist.net/v2/anime/{animeId}/my_list_status",
            new Dictionary<string, string?>
            {
                { "fields", "list_status{status,num_episodes_watched,updated_at}" },
                { "nsfw", "true" }
            }))
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("status", stateStr),
                new("num_watched_episodes", status.WatchedEpisodes.ToString())
            })
        };

        var result = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!HandleStatusCode(result))
            return false;

        try
        {
            using var doc = await JsonDocument.ParseAsync(await result.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
            var statusJson = doc.RootElement;
            anime.Status = StatusFromJson(statusJson);

            UpsertAnime(context, anime);

            context.SaveChanges();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse json from get user list, aborting");
            return false;
        }

        return true;
    }

    private async Task<bool> SaveTokenAsync(HttpResponseMessage result)
    {
        var token = await result.Content.ReadFromJsonAsync<TokenResponse>().ConfigureAwait(false);
        if (token is null)
        {
            _logger.LogError("Couldn't get token from response body");
            return false;
        }

        var newToken = new MyAnimeListToken(token.access_token,
            DateTimeOffset.UtcNow + TimeSpan.FromSeconds(token.expires_in),
            token.refresh_token,
            DateTimeOffset.UtcNow + TimeSpan.FromDays(31));
        await WriteTokenToFileAsync(newToken).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> RefreshTokenAsync(MyAnimeListToken malToken)
    {
        if (malToken.RefreshExpiration < DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5))
        {
            _logger.LogError("Refresh token is expired, deleting token");
            File.Delete(FilePaths.MyAnimeListTokenPath);
            return false;
        }

        if (malToken.AccessExpiration > DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5))
        {
            _logger.LogDebug("No need to refresh token, not expired");
            return true;
        }

        _logger.LogInformation("Refreshing MAL auth token");

        var clientId = _optionsMonitor.CurrentValue.MyAnimeList.ClientId;
        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://myanimelist.net/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("client_id", clientId),
                new("grant_type", "refresh_token"),
                new("refresh_token", malToken.RefreshToken)
            })
        };

        var result = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!HandleStatusCode(result))
            return false;

        if (!await SaveTokenAsync(result).ConfigureAwait(false))
            return false;

        return true;
    }

    private bool HandleStatusCode(HttpResponseMessage result)
    {
        if (!result.IsSuccessStatusCode)
        {
            _logger.LogError("MyAnimeList returned bad status {StatusCode}", result.StatusCode);
            if (result.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("MyAnimeList unauthorized, deleting token");
                File.Delete(FilePaths.MyAnimeListTokenPath);
            }

            return false;
        }

        return true;
    }


    public record MyAnimeListToken(string AccessToken, DateTimeOffset AccessExpiration, string RefreshToken, DateTimeOffset RefreshExpiration);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    private record TokenResponse(string token_type, int expires_in, string access_token, string refresh_token);
}
