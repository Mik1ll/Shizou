using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Common.Enums;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Http.Results;
using Shizou.Server.Services.FileCaches;

namespace Shizou.Server.Commands.AniDb;

public record AnimeArgs(int AnimeId) : CommandArgs($"{nameof(AnimeCommand)}_{AnimeId}");

[Command(CommandType.GetAnime, CommandPriority.Normal, QueueType.AniDbHttp)]
public class AnimeCommand : BaseCommand<AnimeArgs>
{
    private readonly IServiceProvider _provider;
    private readonly string _animeResultCacheKey;
    private readonly ShizouContext _context;
    private readonly HttpAnimeResultCache _animeResultCache;

    public AnimeCommand(IServiceProvider provider, AnimeArgs commandArgs) : base(provider, commandArgs)
    {
        _provider = provider;
        _context = provider.GetRequiredService<ShizouContext>();
        _animeResultCache = provider.GetRequiredService<HttpAnimeResultCache>();
        _animeResultCacheKey = $"AnimeDoc_{CommandArgs.AnimeId}.xml";
    }

    public override async Task Process()
    {
        if (Path.Exists(Path.Combine(_animeResultCache.BasePath, _animeResultCacheKey)) && _animeResultCache.InsideRetentionPeriod(_animeResultCacheKey))
        {
            Logger.LogWarning("Ignoring HTTP anime request: {AnimeId}, already requested in last {Hours} hours", CommandArgs.AnimeId,
                _animeResultCache.RetentionDuration);
            Completed = true;
            return;
        }
        var animeResult = await _animeResultCache.Get(_animeResultCacheKey) ?? await GetAnimeHttp();

        if (animeResult is null)
        {
            Logger.LogWarning("No anime info was returned");
            Completed = true;
            return;
        }

        var aniDbAnime = _context.AniDbAnimes.Include(a => a.AniDbEpisodes)
            .FirstOrDefault(a => a.Id == CommandArgs.AnimeId);
        var mainTitle = animeResult.Titles.First(t => t.Type == "main");
        var newAniDbAnime = new AniDbAnime
        {
            Id = animeResult.Id,
            Description = animeResult.Description,
            Restricted = animeResult.Restricted,
            AirDate = animeResult.Startdate,
            EndDate = animeResult.Enddate,
            AnimeType = animeResult.Type,
            EpisodeCount = animeResult.Episodecount,
            ImagePath = animeResult.Picture,
            Title = mainTitle.Text,
            AniDbEpisodes = animeResult.Episodes.Select(e => new AniDbEpisode
            {
                AniDbAnimeId = animeResult.Id,
                Id = e.Id,
                DurationMinutes = e.Length,
                Number = e.Epno.Text.ParseEpisode().number,
                EpisodeType = e.Epno.Type,
                AirDate = string.IsNullOrEmpty(e.Airdate) ? null : DateTime.Parse(e.Airdate, styles: DateTimeStyles.AssumeUniversal),
                Updated = DateTime.UtcNow,
                TitleEnglish = e.Title.First(t => t.Lang == "en").Text,
                TitleRomaji = e.Title.FirstOrDefault(t => t.Lang.StartsWith("x-") && t.Lang == mainTitle.Lang)?.Text,
                TitleKanji = e.Title.FirstOrDefault(t =>
                    t.Lang.StartsWith(mainTitle.Lang switch { "x-jat" => "ja", "x-zht" => "zh-han", "x-kot" => "ko", _ => "none" },
                        StringComparison.OrdinalIgnoreCase))?.Text
            }).ToList(),
            Updated = DateTime.UtcNow
        };
        if (aniDbAnime is null)
        {
            _context.AniDbAnimes.Add(newAniDbAnime);
        }
        else
        {
            _context.Entry(aniDbAnime).CurrentValues.SetValues(newAniDbAnime);
            _context.ReplaceList(newAniDbAnime.AniDbEpisodes, aniDbAnime.AniDbEpisodes, e => e.Id);
        }

        _context.SaveChanges();
        Completed = true;
    }

    private async Task<HttpAnimeResult?> GetAnimeHttp()
    {
        var request = new AnimeRequest(_provider, CommandArgs.AnimeId);
        await request.Process();
        await _animeResultCache.Save(_animeResultCacheKey, request.ResponseText ?? string.Empty);
        if (request.AnimeResult is null)
            Logger.LogWarning("Failed to get HTTP anime data, retry in {Hours} hours", _animeResultCache.RetentionDuration.Hours);
        return request.AnimeResult;
    }
}
