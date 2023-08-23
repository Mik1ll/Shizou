using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.FileCaches;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public record AnimeArgs(int AnimeId) : CommandArgs($"{nameof(AnimeCommand)}_{AnimeId}");

[Command(CommandType.GetAnime, CommandPriority.Normal, QueueType.AniDbHttp)]
public class AnimeCommand : BaseCommand<AnimeArgs>
{
    private readonly ILogger<AnimeCommand> _logger;
    private readonly ShizouContext _context;
    private readonly HttpAnimeResultCache _animeResultCache;
    private readonly HttpRequestFactory _httpRequestFactory;
    private readonly ImageService _imageService;
    private string _animeResultCacheKey = null!;

    public AnimeCommand(
        ILogger<AnimeCommand> logger,
        ShizouContext context,
        HttpAnimeResultCache animeResultCache,
        HttpRequestFactory httpRequestFactory,
        ImageService imageService)
    {
        _logger = logger;
        _context = context;
        _animeResultCache = animeResultCache;
        _httpRequestFactory = httpRequestFactory;
        _imageService = imageService;
    }

    public override void SetParameters(CommandArgs args)
    {
        _animeResultCacheKey = $"AnimeDoc_{((AnimeArgs)args).AnimeId}.xml";
        base.SetParameters(args);
    }

    protected override async Task ProcessInner()
    {
        if (Path.Exists(Path.Combine(_animeResultCache.BasePath, _animeResultCacheKey)) && _animeResultCache.InsideRetentionPeriod(_animeResultCacheKey))
        {
            _logger.LogWarning("Ignoring HTTP anime request: {AnimeId}, already requested in last {Hours} hours", CommandArgs.AnimeId,
                _animeResultCache.RetentionDuration.TotalHours);
            Completed = true;
            return;
        }

        var animeResult = await _animeResultCache.Get(_animeResultCacheKey) ?? await GetAnimeHttp();

        if (animeResult is null)
        {
            _logger.LogWarning("No anime info was returned");
            Completed = true;
            return;
        }

        var eAniDbAnime = _context.AniDbAnimes.Include(a => a.AniDbEpisodes)
            .FirstOrDefault(a => a.Id == CommandArgs.AnimeId);
        var aniDbAnime = AnimeResultToAniDbAnime(animeResult);
        if (eAniDbAnime is null)
        {
            _context.AniDbAnimes.Add(aniDbAnime);
        }
        else
        {
            _context.Entry(eAniDbAnime).CurrentValues.SetValues(aniDbAnime);
            foreach (var e in eAniDbAnime.AniDbEpisodes.ExceptBy(aniDbAnime.AniDbEpisodes.Select(x => x.Id), x => x.Id))
                eAniDbAnime.AniDbEpisodes.Remove(e);
            foreach (var e in aniDbAnime.AniDbEpisodes)
                if (eAniDbAnime.AniDbEpisodes.FirstOrDefault(x => x.Id == e.Id) is var ee && ee is null)
                    eAniDbAnime.AniDbEpisodes.Add(e);
                else
                    _context.Entry(ee).CurrentValues.SetValues(e);
        }

        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();

        if (aniDbAnime.ImageFilename is not null)
            _imageService.GetAnimePoster(aniDbAnime.Id);

        UpdateMalXrefs(animeResult);

        Completed = true;
    }

    private static AniDbAnime AnimeResultToAniDbAnime(AnimeResult animeResult)
    {
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
            ImageFilename = animeResult.Picture,
            Title = mainTitle.Text,
            AniDbEpisodes = animeResult.Episodes.Select(e => new AniDbEpisode
            {
                AniDbAnimeId = animeResult.Id,
                Id = e.Id,
                DurationMinutes = e.Length,
                Number = EpisodeTypeExtensions.ParseEpisode(e.Epno.Text).number,
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
        return newAniDbAnime;
    }

    private async Task<AnimeResult?> GetAnimeHttp()
    {
        var request = _httpRequestFactory.AnimeRequest(CommandArgs.AnimeId);
        await request.Process();
        await _animeResultCache.Save(_animeResultCacheKey, request.ResponseText ?? string.Empty);
        if (request.AnimeResult is null)
            _logger.LogWarning("Failed to get HTTP anime data, retry in {Hours} hours", _animeResultCache.RetentionDuration.Hours);
        return request.AnimeResult;
    }

    private void UpdateMalXrefs(AnimeResult animeResult)
    {
        var xrefs = _context.MalAniDbXrefs.Where(xref => xref.AniDbId == CommandArgs.AnimeId).ToList();
        _context.RemoveRange(xrefs);

        var malIds = animeResult.Resources.Where(r => (ResourceType)r.Type == ResourceType.Mal)
            .SelectMany(r => r.ExternalEntities.SelectMany(e => e.Identifiers).Select(int.Parse)).ToList();

        _context.MalAniDbXrefs.AddRange(malIds.Select(id => new MalAniDbXref { AniDbId = CommandArgs.AnimeId, MalId = id }));
        _context.SaveChanges();
    }
}