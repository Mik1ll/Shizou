using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Extensions;
using Timer = Shizou.Data.Models.Timer;

namespace Shizou.Server.Services;

public class AnimeTitleSearchService : IAnimeTitleSearchService
{
    private readonly Regex _removeSpecial = new(@"[][【】「」『』、…〜（）`()\\,<>/;:：'""-]+", RegexOptions.Compiled);
    private readonly ILogger<AnimeTitleSearchService> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IShizouContextFactory _contextFactory;
    private readonly CommandService _commandService;
    private List<AnimeTitle>? _animeTitlesMemCache;

    public AnimeTitleSearchService(
        ILogger<AnimeTitleSearchService> logger,
        IHttpClientFactory clientFactory,
        IShizouContextFactory contextFactory,
        CommandService commandService)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _contextFactory = contextFactory;
        _commandService = commandService;
    }

    /// <summary>
    ///     Search for an anime by title
    /// </summary>
    /// <param name="query">Search Query</param>
    /// <param name="searchSpace">Anime IDs to search</param>
    /// <returns>A list of anime Ids and their titles, sorted by relevance</returns>
    public async Task<List<(int, string)>?> SearchAsync(string query, HashSet<int>? searchSpace = null)
    {
        if (_animeTitlesMemCache is null)
            await GetTitlesAsync().ConfigureAwait(false);

        if (_animeTitlesMemCache is null)
            return null;
        return SearchTitles(searchSpace is null ? _animeTitlesMemCache : _animeTitlesMemCache.Where(a => searchSpace.Contains(a.Aid)), query)
            .Select(t => (t.Aid, t.Title)).ToList();
    }

    /// <summary>
    ///     Try to retrieve the complete list of anime titles from AniDB into the in-memory cache
    ///     If titles have been requested recently, retrieve from file cache
    /// </summary>
    public async Task GetTitlesAsync()
    {
        string? data;
        using var context = _contextFactory.CreateDbContext();
        // ReSharper disable once MethodHasAsyncOverload
        // ReSharper disable once UseAwaitUsing
        using var trans = context.Database.BeginTransaction();
        var timer = context.Timers.FirstOrDefault(t => t.Type == TimerType.AnimeTitlesRequest);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            if (_animeTitlesMemCache is null)
            {
                _logger.LogInformation("Anime titles requested recently, getting from local file");
                data = await GetFromFileAsync().ConfigureAwait(false);
            }
            else
            {
                _logger.LogDebug("Anime titles request recently, using in-memory cache");
                return;
            }
        }
        else
        {
            var rateLimit = TimeSpan.FromDays(1);
            var rateLimitExpires = DateTime.UtcNow + rateLimit;
            if (timer is not null)
                timer.Expires = rateLimitExpires;
            else
                context.Timers.Add(new Timer
                {
                    Type = TimerType.AnimeTitlesRequest,
                    Expires = rateLimitExpires
                });

            context.SaveChanges();
            // ReSharper disable once MethodHasAsyncOverload
            trans.Commit();
            data = await GetFromAniDbAsync().ConfigureAwait(false);
            ScheduleNextUpdate();
        }

        if (data is null)
            return;
        _animeTitlesMemCache = ParseContent(data);
    }

    /// <summary>
    ///     Retrieve AniDB titles response from the file cache
    /// </summary>
    /// <returns>Cached AniDB anime titles response content</returns>
    private async Task<string?> GetFromFileAsync()
    {
        if (File.Exists(FilePaths.AnimeTitlesPath))
            return await File.ReadAllTextAsync(FilePaths.AnimeTitlesPath, Encoding.UTF8).ConfigureAwait(false);
        _logger.LogError("Could not find anime titles file at \"{AnimeTitlesPath}\"", FilePaths.AnimeTitlesPath);
        return null;
    }

    /// <summary>
    ///     Retrieve AniDB titles from the AniDB api
    /// </summary>
    /// <returns>The AniDB titles response content or null if there was an error response</returns>
    private async Task<string?> GetFromAniDbAsync()
    {
        // Not using gzip client, didn't work. Maybe it requires a gzip content header in the response
        var client = _clientFactory.CreateClient(string.Empty);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Shizou");
        using var response = await client.GetAsync("https://anidb.net/api/anime-titles.dat.gz").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anime titles request failed: {StatusCode} {StatusMessage}", response.StatusCode, response.ReasonPhrase);
            return null;
        }

        var httpStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await using var _ = httpStream.ConfigureAwait(false);
        var decompStream = new GZipStream(httpStream, CompressionMode.Decompress);
        await using var __ = decompStream.ConfigureAwait(false);
        using var reader = new StreamReader(decompStream);
        var data = await reader.ReadToEndAsync().ConfigureAwait(false);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.AnimeTitlesPath)!);
        await File.WriteAllTextAsync(FilePaths.AnimeTitlesPath, data, Encoding.UTF8).ConfigureAwait(false);
        return data;
    }

    /// <summary>
    ///     Parse the AniDB titles response content into a list of <see cref="AnimeTitle" />
    /// </summary>
    /// <param name="content">AniDB titles response content</param>
    /// <returns>A list of <see cref="AnimeTitle" /></returns>
    private List<AnimeTitle> ParseContent(string content)
    {
        var titles = new List<AnimeTitle>();
        foreach (var line in content.SplitSpan('\n'))
        {
            if (line.IsEmpty || line.StartsWith("#"))
                continue;
            var enumerator = line.SplitSpan('|');
            enumerator.MoveNext();
            var aid = int.Parse(enumerator.Current);
            enumerator.MoveNext();
            // var type = Enum.Parse<TitleType>(enumerator.Current);
            enumerator.MoveNext();
            // var lang = enumerator.Current.ToString();
            enumerator.MoveNext();
            titles.Add(new AnimeTitle(aid, enumerator.Current.ToString(), CleanTitle(enumerator.Current.ToString())));
        }

        return titles;
    }

    /// <summary>
    ///     Prepare Anime Title for better searchability
    /// </summary>
    /// <param name="title">Anime title</param>
    /// <returns>Cleaned Anime title</returns>
    private string CleanTitle(string title) => _removeSpecial.Replace(title.ToLowerInvariant(), "").Trim();

    /// <summary>
    ///     Search the titles with the given query
    /// </summary>
    /// <param name="titles">The titles to search</param>
    /// <param name="query">The query to use when searching</param>
    /// <returns>Search Results ordered by relevance score</returns>
    private List<AnimeTitle> SearchTitles(IEnumerable<AnimeTitle> titles, string query)
    {
        var scorer = ScorerCache.Get<PartialRatioScorer>();

        query = CleanTitle(query);
        var results = Process.ExtractTop(new AnimeTitle(0, "", query), titles, p => p.ProcessedTitle, scorer, 50, 70).ToList();
        var refinedResults = results.GroupBy(r => r.Value.Aid)
            .Select(g => g.OrderByDescending(r => r.Score).First())
            .OrderByDescending(r => r.Score).ToList();

        return refinedResults.Select(r => r.Value).ToList();
    }

    /// <summary>
    ///     Schedule the next anime titles request after the rate limit timer expires
    /// </summary>
    public void ScheduleNextUpdate()
    {
        using var context = _contextFactory.CreateDbContext();
        var timer = context.Timers.FirstOrDefault(t => t.Type == TimerType.AnimeTitlesRequest);
        _commandService.ScheduleCommand(new GetAnimeTitlesArgs(), 1,
            (timer?.Expires > DateTimeOffset.UtcNow ? timer.Expires : DateTimeOffset.UtcNow) + TimeSpan.FromSeconds(10), null, true);
    }

    private record AnimeTitle(int Aid, string Title, string ProcessedTitle);
}
