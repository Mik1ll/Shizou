using System;
using System.Globalization;
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
    private string _animeCacheFilename = null!;

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
        _animeCacheFilename = $"AnimeDoc_{((AnimeArgs)args).AnimeId}.xml";
        base.SetParameters(args);
    }

    protected override async Task ProcessInner()
    {
        var animeResult = await GetAnime();

        if (animeResult is null)
        {
            _logger.LogWarning("No anime info was returned");
            Completed = true;
            return;
        }

        var eAniDbAnime = _context.AniDbAnimes.Include(a => a.AniDbEpisodes).ThenInclude(ep => ep.EpisodeWatchedState)
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
        {
            // ReSharper disable once MethodHasAsyncOverload
            if (_context.AniDbEpisodes.Find(ep.Id) is { } eEp)
                _context.Entry(eEp).CurrentValues.SetValues(ep);
            else
                _context.AniDbEpisodes.Add(ep);
            foreach (var eHangingXref in _context.HangingEpisodeFileXrefs.Where(x => x.AniDbEpisodeId == ep.Id))
            {
                if (!_context.AniDbEpisodeFileXrefs.Any(x => x.AniDbEpisodeId == ep.Id && x.AniDbFileId == eHangingXref.AniDbFileId))
                    _context.AniDbEpisodeFileXrefs.Add(new AniDbEpisodeFileXref
                    {
                        AniDbEpisodeId = ep.Id,
                        AniDbFileId = eHangingXref.AniDbFileId
                    });
                _context.HangingEpisodeFileXrefs.Remove(eHangingXref);
            }
        }

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
                Number = EpisodeTypeExtensions.ParseEpisode(e.Epno.Text)
                    .number,
                EpisodeType = e.Epno.Type,
                AirDate = string.IsNullOrEmpty(e.Airdate)
                    ? null
                    : DateTime.Parse(e.Airdate,
                        styles: DateTimeStyles.AssumeUniversal),
                Updated = DateTime.UtcNow,
                TitleEnglish = e.Title.First(t => t.Lang == "en")
                    .Text,
                TitleRomaji = e.Title.FirstOrDefault(t => t.Lang.StartsWith("x-") && t.Lang == mainTitle.Lang)
                    ?.Text,
                TitleKanji = e.Title.FirstOrDefault(t =>
                        t.Lang.StartsWith(mainTitle.Lang switch
                            {
                                "x-jat" => "ja",
                                "x-zht" => "zh-han",
                                "x-kot" => "ko",
                                _ => "none"
                            },
                            StringComparison.OrdinalIgnoreCase))
                    ?.Text,
                EpisodeWatchedState = new EpisodeWatchedState
                {
                    AniDbEpisodeId = e.Id,
                    Watched = false,
                    WatchedUpdated = null
                }
            }).ToList(),
            Updated = DateTime.UtcNow
        };
        return newAniDbAnime;
    }

    private async Task<AnimeResult?> GetAnime()
    {
        // ReSharper disable once MethodHasAsyncOverload
        var timer = _context.Timers.FirstOrDefault(t => t.Type == TimerType.AnimeRequest && t.ExtraId == CommandArgs.AnimeId);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            _logger.LogWarning("Anime {AnimeId} already requested recently, trying cache for HTTP anime request", CommandArgs.AnimeId);
            return await _animeResultCache.Get(_animeCacheFilename);
        }

        var rateLimit = TimeSpan.FromDays(1);
        var rateLimitExpires = DateTime.UtcNow + rateLimit;
        if (timer is not null)
            timer.Expires = rateLimitExpires;
        else
            _context.Timers.Add(new Timer
            {
                Type = TimerType.AnimeRequest,
                ExtraId = CommandArgs.AnimeId,
                Expires = rateLimitExpires
            });
        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();
        _logger.LogInformation("Getting Anime {AnimeId} from HTTP anime request", CommandArgs.AnimeId);
        _animeRequest.SetParameters(CommandArgs.AnimeId);
        await _animeRequest.Process();
        await _animeResultCache.Save(_animeCacheFilename, _animeRequest.ResponseText ?? string.Empty);
        if (_animeRequest.AnimeResult is null)
            _logger.LogWarning("Failed to get HTTP anime data, retry in {Hours} hours", rateLimit.TotalHours);
        return _animeRequest.AnimeResult;
    }

    private async Task UpdateMalXrefs(AnimeResult animeResult)
    {
        var xrefs = animeResult.Resources.Where(r => (ResourceType)r.Type == ResourceType.Mal)
            .SelectMany(r => r.ExternalEntities.SelectMany(e => e.Identifiers)
                .Select(id => new MalAniDbXref { AniDbAnimeId = CommandArgs.AnimeId, MalAnimeId = int.Parse(id) })).ToList();
        foreach (var xref in xrefs)
            await _myAnimeListService.GetAnime(xref.MalAnimeId);
        var eXrefs = _context.MalAniDbXrefs.Where(xref => xref.AniDbAnimeId == CommandArgs.AnimeId).ToList();
        _context.RemoveRange(eXrefs.ExceptBy(xrefs.Select(x => x.MalAnimeId), x => x.MalAnimeId));
        _context.AddRange(xrefs.ExceptBy(eXrefs.Select(x => x.MalAnimeId), x => x.MalAnimeId));

        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();
    }
}
