﻿using System;
using System.Collections.Generic;
using System.Globalization;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Data.Utilities.Extensions;

namespace Shizou.Server.Services;

public class AnimeTitleSearchService
{
    private readonly Regex _removeSpecial = new(@"[][【】「」『』、…〜（）`()\\,<>/;:：'""-]+", RegexOptions.Compiled);
    private readonly ILogger<AnimeTitleSearchService> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private List<AnimeTitle>? _animeTitlesMemCache;

    public AnimeTitleSearchService(
        ILogger<AnimeTitleSearchService> logger,
        IHttpClientFactory clientFactory,
        IDbContextFactory<ShizouContext> contextFactory)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _contextFactory = contextFactory;
    }

    private enum TitleType
    {
        Primary = 1,
        Synonym = 2,
        Short = 3,
        Official = 4
    }

    public async Task<List<(int, string)>?> Search(string query)
    {
        await GetTitles();
        if (_animeTitlesMemCache is null)
            return null;
        return SearchTitles(_animeTitlesMemCache, query).Select(t => (t.Aid, t.Title)).ToList();
    }

    private async Task GetTitles()
    {
        string? data;
        // ReSharper disable once MethodHasAsyncOverload
        // ReSharper disable once UseAwaitUsing
        using var context = _contextFactory.CreateDbContext();
        var timer = context.Timers.FirstOrDefault(t => t.Type == TimerType.AnimeTitlesRequest);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            if (_animeTitlesMemCache is null)
            {
                _logger.LogInformation("Anime titles requested recently, getting from local file");
                data = await GetFromFile();
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
            // ReSharper disable once MethodHasAsyncOverload
            context.SaveChanges();
            data = await GetFromAniDb();
        }

        if (data is null)
            return;
        _animeTitlesMemCache = ParseContent(data);
    }

    private async Task<string?> GetFromFile()
    {
        if (File.Exists(FilePaths.AnimeTitlesPath))
            return await File.ReadAllTextAsync(FilePaths.AnimeTitlesPath, Encoding.UTF8);
        _logger.LogError("Could not find anime titles file at \"{AnimeTitlesPath}\"", FilePaths.AnimeTitlesPath);
        return null;
    }

    private async Task<string?> GetFromAniDb()
    {
        // Not using gzip client, didn't work. Maybe it requires a gzip content header in the response
        var client = _clientFactory.CreateClient(string.Empty);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Shizou");
        using var response = await client.GetAsync("https://anidb.net/api/anime-titles.dat.gz");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anime titles request failed: {StatusCode} {StatusMessage}", response.StatusCode, response.ReasonPhrase);
            return null;
        }

        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await using var decompStream = new GZipStream(httpStream, CompressionMode.Decompress);
        using var reader = new StreamReader(decompStream);
        var data = await reader.ReadToEndAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.AnimeTitlesPath)!);
        await File.WriteAllTextAsync(FilePaths.AnimeTitlesPath, data, Encoding.UTF8);
        return data;
    }

    private List<AnimeTitle> ParseContent(string content)
    {
        var titles = new List<AnimeTitle>();
        var titleSpan = new Span<char>(new char[200]);
        foreach (var line in content.SplitSpan('\n'))
        {
            if (line.IsEmpty || line.StartsWith(new ReadOnlySpan<char>('#')))
                continue;
            var enumerator = line.SplitSpan('|');
            enumerator.MoveNext();
            var aid = int.Parse(enumerator.Current);
            enumerator.MoveNext();
            var type = Enum.Parse<TitleType>(enumerator.Current);
            enumerator.MoveNext();
            var lang = enumerator.Current.ToString();
            var culture = lang.Length <= 3 ? CultureInfo.GetCultureInfoByIetfLanguageTag(lang) : CultureInfo.InvariantCulture;
            enumerator.MoveNext();
            titleSpan.Clear();
            enumerator.Current.ToLower(titleSpan, culture);
            foreach (var match in _removeSpecial.EnumerateMatches(titleSpan))
                titleSpan.Slice(match.Index, match.Length).Fill(' ');
            titleSpan.Trim();
            var firstNull = titleSpan.IndexOf('\0');
            titles.Add(new AnimeTitle(aid, type, lang, enumerator.Current.ToString(),
                titleSpan.Slice(0, Math.Min(firstNull > 0 ? firstNull : int.MaxValue, titleSpan.Length)).ToString()));
        }

        return titles;
    }

    private List<AnimeTitle> SearchTitles(List<AnimeTitle> titles, string query)
    {
        query = _removeSpecial.Replace(query.ToLower(CultureInfo.CurrentUICulture), " ").Trim();
        var results = Process.ExtractTop(
            new AnimeTitle(0, TitleType.Primary, "", "", query),
            titles, p => p.ProcessedTitle, ScorerCache.Get<TokenSetScorer>(), 50, 70).ToList();
        var refinedResults = results.GroupBy(r => r.Value.Aid)
            .Select(g => g
                .OrderBy(r => r.Value.Type switch
                {
                    TitleType.Primary => 0,
                    TitleType.Official => 1,
                    TitleType.Synonym => 2,
                    TitleType.Short => 3,
                    _ => int.MaxValue
                })
                .ThenBy(r => r.Value.Lang switch
                {
                    var x when x.StartsWith("x-") => 0,
                    "en" => 1,
                    "ja" => 2,
                    "ko" => 2,
                    var x when x.StartsWith("zh") => 2,
                    _ => int.MaxValue
                })
                .ThenByDescending(r => r.Score).First())
            .OrderByDescending(r => r.Score).ToList();
        return refinedResults.Select(r => r.Value).ToList();
    }

    private record AnimeTitle(int Aid, TitleType Type, string Lang, string Title, string ProcessedTitle);
}
