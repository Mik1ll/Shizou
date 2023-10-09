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
    private static readonly Regex RemoveSpecial = new(@"[][`!@#$%^&*()+_=\\.,<>?/;:'""-]", RegexOptions.Compiled);
    private readonly ILogger<AnimeTitleSearchService> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

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
        var data = await GetContent();
        if (data is null) return null;
        var titles = ParseContent(data);
        return SearchTitles(titles, query).Select(t => (t.Aid, t.Title)).ToList();
    }

    private async Task<string?> GetContent()
    {
        // ReSharper disable once MethodHasAsyncOverload
        // ReSharper disable once UseAwaitUsing
        using var context = _contextFactory.CreateDbContext();
        var timer = context.Timers.FirstOrDefault(t => t.Type == TimerType.AnimeTitlesRequest);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            _logger.LogInformation("Anime titles requested recently, getting from local file");
            if (File.Exists(FilePaths.AnimeTitlesPath))
                return await File.ReadAllTextAsync(FilePaths.AnimeTitlesPath, Encoding.UTF8);
            _logger.LogError("Could not find anime titles file at \"{AnimeTitlesPath}\"", FilePaths.AnimeTitlesPath);
            return null;
        }

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
            var lang = enumerator.Current;
            enumerator.MoveNext();
            var title = enumerator.Current;
            titles.Add(new AnimeTitle(aid, type, lang.ToString(), title.ToString()));
        }

        return titles;
    }

    private List<AnimeTitle> SearchTitles(List<AnimeTitle> titles, string query)
    {
        string Processor(AnimeTitle title)
        {
            return RemoveSpecial.Replace(title.Title.ToLower().Trim(), string.Empty);
        }

        var results = Process.ExtractTop(new AnimeTitle(0, TitleType.Primary, "", query), titles, Processor, ScorerCache.Get<TokenSetScorer>(), 50).ToList();
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
                    "x-jat" => 0,
                    "x-kot" => 0,
                    "x-zht" => 0,
                    "x-other" => 0,
                    "x-unk" => 0,
                    "en" => 1,
                    "ja" => 2,
                    "ko" => 2,
                    "zh" => 2,
                    "zh-Hans" => 2,
                    _ => int.MaxValue
                })
                .ThenByDescending(r => r.Score).First())
            .OrderByDescending(r => r.Score).Select(r => r.Value).ToList();
        return refinedResults;
    }

    private record AnimeTitle(int Aid, TitleType Type, string Lang, string Title);
}
