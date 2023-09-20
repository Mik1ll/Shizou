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
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.FileCaches;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public record AnimeArgs(int AnimeId) : CommandArgs($"{nameof(AnimeCommand)}_{AnimeId}");

[Command(CommandType.GetAnime, CommandPriority.Normal, QueueType.AniDbHttp)]
public class AnimeCommand : Command<AnimeArgs>
{
    private readonly ILogger<AnimeCommand> _logger;
    private readonly ShizouContext _context;
    private readonly HttpAnimeResultCache _animeResultCache;
    private readonly ImageService _imageService;
    private readonly MyAnimeListService _myAnimeListService;
    private readonly IAnimeRequest _animeRequest;
    private string _animeResultCacheKey = null!;

    public AnimeCommand(
        ILogger<AnimeCommand> logger,
        ShizouContext context,
        HttpAnimeResultCache animeResultCache,
        ImageService imageService,
        MyAnimeListService myAnimeListService,
        IAnimeRequest animeRequest)
    {
        _logger = logger;
        _context = context;
        _animeResultCache = animeResultCache;
        _imageService = imageService;
        _myAnimeListService = myAnimeListService;
        _animeRequest = animeRequest;
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
            _context.Entry(aniDbAnime).State = EntityState.Added;
        }
        else
        {
            _context.Entry(eAniDbAnime).CurrentValues.SetValues(aniDbAnime);
            foreach (var ep in eAniDbAnime.AniDbEpisodes.ExceptBy(aniDbAnime.AniDbEpisodes.Select(ep => ep.Id), ep => ep.Id))
                eAniDbAnime.AniDbEpisodes.Remove(ep);
        }

        foreach (var ep in aniDbAnime.AniDbEpisodes)
            if (eAniDbAnime?.AniDbEpisodes.FirstOrDefault(x => x.Id == ep.Id) is { } eEp)
                _context.Entry(eEp).CurrentValues.SetValues(ep);
            else
                _context.AniDbEpisodes.Add(ep);

        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();

        foreach (var epId in _context.AniDbEpisodes.Where(ep =>
                         ep.AniDbAnimeId == aniDbAnime.Id && !_context.EpisodeWatchedStates.Any(ws => ws.Id == ep.Id))
                     .Select(ep => ep.Id))
            _context.EpisodeWatchedStates.Add(new EpisodeWatchedState { Id = epId, Watched = false, WatchedUpdated = null });

        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();

        if (aniDbAnime.ImageFilename is not null)
            _imageService.GetAnimePoster(aniDbAnime.Id);

        await UpdateMalXrefs(animeResult);

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
        _animeRequest.SetParameters(CommandArgs.AnimeId);
        await _animeRequest.Process();
        await _animeResultCache.Save(_animeResultCacheKey, _animeRequest.ResponseText ?? string.Empty);
        if (_animeRequest.AnimeResult is null)
            _logger.LogWarning("Failed to get HTTP anime data, retry in {Hours} hours", _animeResultCache.RetentionDuration.Hours);
        return _animeRequest.AnimeResult;
    }

    private async Task UpdateMalXrefs(AnimeResult animeResult)
    {
        var eXrefs = _context.MalAniDbXrefs.Where(xref => xref.AniDbId == CommandArgs.AnimeId).ToList();

        var xrefs = animeResult.Resources.Where(r => (ResourceType)r.Type == ResourceType.Mal)
            .SelectMany(r => r.ExternalEntities.SelectMany(e => e.Identifiers)
                .Select(id => new MalAniDbXref { AniDbId = CommandArgs.AnimeId, MalId = int.Parse(id) })).ToList();
        _context.RemoveRange(eXrefs.ExceptBy(xrefs.Select(x => x.MalId), x => x.MalId));
        _context.AddRange(xrefs.ExceptBy(eXrefs.Select(x => x.MalId), x => x.MalId));
        foreach (var xref in xrefs.Where(x => !_context.MalAnimes.Any(a => a.Id == x.MalId)))
            await _myAnimeListService.GetAnime(xref.MalId);

        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();
    }
}