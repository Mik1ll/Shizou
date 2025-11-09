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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums.Mal;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Options;
using Base64UrlTextEncoder = Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder;

namespace Shizou.Server.Services;

public class MalAuthorization
{
    private static readonly Dictionary<string, (string challengeAndVerifier, string redirectUri)> AuthFlows = new();
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LinkGenerator _linkGenerator;

    public MalAuthorization(IOptionsMonitor<ShizouOptions> optionsMonitor, ILogger logger, IHttpClientFactory httpClientFactory, LinkGenerator linkGenerator)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _linkGenerator = linkGenerator;
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

    public string? GetAuthenticationUrl(Uri baseUri)
    {
        var options = _optionsMonitor.CurrentValue.MyAnimeList;
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            _logger.LogError("Tried to authenticate with MAL without a client ID");
            return null;
        }

        var codeChallengeAndVerifier = GetCodeVerifier();
        var state = Guid.NewGuid().ToString();
        var redirectUri = _linkGenerator.GetUriByAction(nameof(MyAnimeList.GetToken), nameof(MyAnimeList), null,
                              baseUri.Scheme, new HostString(baseUri.Authority), new PathString(baseUri.AbsolutePath)) ??
                          throw new ArgumentException("Could not generate MAL GetToken uri");

        AuthFlows[state] = (codeChallengeAndVerifier, redirectUri);

        var url = QueryHelpers.AddQueryString("https://myanimelist.net/v1/oauth2/authorize", new Dictionary<string, string?>
        {
            { "response_type", "code" },
            { "client_id", options.ClientId },
            { "state", state },
            { "code_challenge", codeChallengeAndVerifier },
            { "code_challenge_method", "plain" },
            { "redirect_uri", redirectUri },
        });
        _logger.LogInformation("Created MAL auth flow url {Url}", url);
        return url;
    }

    public async Task<bool> GetNewTokenAsync(string code, string state)
    {
        _logger.LogInformation("Got auth code, requesting new tokens");
        var options = _optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.MyAnimeList.ClientId))
        {
            _logger.LogError("Tried to authenticate with MAL without a client ID");
            return false;
        }

        if (!AuthFlows.Remove(state, out var tuple))
        {
            _logger.LogError("State not found in auth flows");
            return false;
        }

        var (challengeAndVerifier, redirectUri) = tuple;

        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://myanimelist.net/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("client_id", options.MyAnimeList.ClientId),
                new("grant_type", "authorization_code"),
                new("code", code),
                new("code_verifier", challengeAndVerifier),
                new("redirect_uri", redirectUri),
            }),
        };
        var result = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!HandleStatusCode(result))
            return false;

        if (await TokenFromResponseAsync(result).ConfigureAwait(false) is not { } newToken)
            return false;

        await WriteTokenToFileAsync(newToken).ConfigureAwait(false);

        return true;
    }

    public async Task<MyAnimeListToken?> GetTokenRefreshIfRequiredAsync()
    {
        var malToken = await ReadTokenFromFileAsync().ConfigureAwait(false);
        if (malToken is null)
        {
            _logger.LogWarning("Unable to get token, none saved");
            return null;
        }

        if (malToken.RefreshExpiration < DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5))
        {
            _logger.LogError("Refresh token is expired, deleting token");
            File.Delete(FilePaths.MyAnimeListTokenPath);
            return null;
        }

        if (malToken.AccessExpiration > DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5))
        {
            _logger.LogDebug("No need to refresh token, not expired");
            return malToken;
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
                new("refresh_token", malToken.RefreshToken),
            }),
        };

        var result = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!HandleStatusCode(result))
            return null;

        if (await TokenFromResponseAsync(result).ConfigureAwait(false) is not { } newToken)
            return null;

        await WriteTokenToFileAsync(newToken).ConfigureAwait(false);

        return newToken;
    }

    public bool HandleStatusCode(HttpResponseMessage result)
    {
        if (result.IsSuccessStatusCode) return true;
        _logger.LogError("MyAnimeList returned bad status {StatusCode}", result.StatusCode);
        if (result.StatusCode != HttpStatusCode.Unauthorized) return false;
        _logger.LogError("MyAnimeList unauthorized, deleting token");
        File.Delete(FilePaths.MyAnimeListTokenPath);
        return false;
    }

    private async Task WriteTokenToFileAsync(MyAnimeListToken newToken)
    {
        _logger.LogInformation("Saving new MAL token to file");
        var tokenStream = new FileStream(FilePaths.MyAnimeListTokenPath, FileMode.Create, FileAccess.Write);
        await using (tokenStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(tokenStream, newToken).ConfigureAwait(false);
        }
    }

    private async Task<MyAnimeListToken?> TokenFromResponseAsync(HttpResponseMessage result)
    {
        var token = await result.Content.ReadFromJsonAsync<TokenResponse>().ConfigureAwait(false);
        if (token is null)
        {
            _logger.LogError("Couldn't get token from response body");
            return null;
        }

        var newToken = new MyAnimeListToken(token.access_token,
            DateTimeOffset.UtcNow + TimeSpan.FromHours(1),
            token.refresh_token,
            DateTimeOffset.UtcNow + TimeSpan.FromSeconds(token.expires_in));
        return newToken;
    }

    public record MyAnimeListToken(string AccessToken, DateTimeOffset AccessExpiration, string RefreshToken, DateTimeOffset RefreshExpiration);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("Style", "IDE1006:Naming Styles")]
    private record TokenResponse(string token_type, int expires_in, string access_token, string refresh_token);
}

public class MyAnimeListService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MyAnimeListService> _logger;
    private readonly IShizouContextFactory _contextFactory;

    public MyAnimeListService(
        ILogger<MyAnimeListService> logger,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        IShizouContextFactory contextFactory, LinkGenerator linkGenerator)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        MalAuthorization = new MalAuthorization(optionsMonitor, logger, httpClientFactory, linkGenerator);
        _contextFactory = contextFactory;
    }

    public MalAuthorization MalAuthorization { get; }

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

    public async Task<MalAnime?> GetAnimeAsync(int animeId)
    {
        if (await MalAuthorization.GetTokenRefreshIfRequiredAsync().ConfigureAwait(false) is not { } malToken)
            return null;

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", malToken.AccessToken);

        var url = QueryHelpers.AddQueryString($"https://api.myanimelist.net/v2/anime/{animeId}", new Dictionary<string, string?>
        {
            { "nsfw", "true" },
            { "fields", "id,title,media_type,num_episodes,my_list_status{status,num_episodes_watched,updated_at}" },
        });

        _logger.LogInformation("Getting MyAnimeList Anime: {AnimeId}", animeId);
        var result = await httpClient.GetAsync(url).ConfigureAwait(false);
        if (!MalAuthorization.HandleStatusCode(result))
            return null;
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
            return anime;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse json from get user list, aborting");
            return null;
        }
    }

    public async Task GetUserAnimeListAsync()
    {
        if (await MalAuthorization.GetTokenRefreshIfRequiredAsync().ConfigureAwait(false) is not { } malToken)
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
            { "limit", "1000" },
        });
        _logger.LogInformation("Getting entries from user's MyAnimeList anime list");
        while (url is not null)
        {
            var result = await httpClient.GetAsync(url).ConfigureAwait(false);
            if (!MalAuthorization.HandleStatusCode(result))
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
                if (url is not null)
                    _logger.LogInformation("Getting next page of entries from user's MyAnimeList anime list");
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
        if (await MalAuthorization.GetTokenRefreshIfRequiredAsync().ConfigureAwait(false) is not { } malToken)
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
            _ => throw new ArgumentOutOfRangeException(nameof(status.State), status.State, null),
        };

        var request = new HttpRequestMessage(HttpMethod.Patch, QueryHelpers.AddQueryString($"https://api.myanimelist.net/v2/anime/{animeId}/my_list_status",
            new Dictionary<string, string?>
            {
                { "fields", "list_status{status,num_episodes_watched,updated_at}" },
                { "nsfw", "true" },
            }))
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("status", stateStr),
                new("num_watched_episodes", status.WatchedEpisodes.ToString()),
            }),
        };

        _logger.LogInformation("Sending status update for MyAnimeList entry: id: {AnimeId}, state: {State}, watched eps: {Watched}", animeId, stateStr,
            status.WatchedEpisodes);
        var result = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!MalAuthorization.HandleStatusCode(result))
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
}
