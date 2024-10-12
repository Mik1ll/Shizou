using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Extensions;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.Options;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class AnimeCommand : Command<AnimeArgs>
{
    private readonly ILogger<AnimeCommand> _logger;
    private readonly IShizouContext _context;
    private readonly ImageService _imageService;
    private readonly MyAnimeListService _myAnimeListService;
    private readonly IAnimeRequest _animeRequest;
    private readonly CommandService _commandService;
    private readonly SymbolicCollectionViewService _collectionViewService;
    private readonly ShizouOptions _options;

    public AnimeCommand(
        ILogger<AnimeCommand> logger,
        IShizouContext context,
        ImageService imageService,
        MyAnimeListService myAnimeListService,
        IAnimeRequest animeRequest,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        SymbolicCollectionViewService collectionViewService)
    {
        _logger = logger;
        _context = context;
        _imageService = imageService;
        _myAnimeListService = myAnimeListService;
        _animeRequest = animeRequest;
        _commandService = commandService;
        _collectionViewService = collectionViewService;
        _options = optionsSnapshot.Value;
    }

    private static AniDbAnime AnimeResultToAniDbAnime(AnimeResult animeResult)
    {
        var mainTitle = animeResult.Titles.First(t => t.Type == "main");
        var originalLangPrefix = mainTitle.Lang switch
        {
            "x-jat" => "ja",
            "x-zht" => "zh-Han",
            "x-kot" => "ko",
            "x-tht" => "th",
            _ => "none"
        };
        var tags = animeResult.Tags.ToDictionary(t => t.Id);
        var filteredTags = tags.Values.Where(t =>
        {
            if (!t.ParentidSpecified || (t.Weight < 300 && t.Weight != 0))
                return false;
            var pTagId = t.Parentid;
            while (tags[pTagId].ParentidSpecified)
                pTagId = tags[pTagId].Parentid;
            if (tags[pTagId].Id is 2611 or 2607 or 2606)
                return true;
            return false;
        }).Select(t => t.Name).ToList();
        var newAniDbAnime = new AniDbAnime
        {
            Id = animeResult.Id,
            Description = animeResult.Description,
            Restricted = animeResult.Restricted,
            AirDate = animeResult.Startdate,
            EndDate = animeResult.Enddate,
            AnimeType = animeResult.Type,
            EpisodeCount = animeResult.Episodecount == 0 ? null : animeResult.Episodecount,
            ImageFilename = animeResult.Picture,
            TitleTranscription = mainTitle.Text,
            TitleOriginal = animeResult.Titles
                .FirstOrDefault(t => t.Type == "official" && t.Lang.StartsWith(originalLangPrefix, StringComparison.OrdinalIgnoreCase))
                ?.Text,
            TitleEngish = animeResult.Titles.FirstOrDefault(t => t is { Type: "official", Lang: "en" })?.Text,
            Rating = animeResult.Ratings?.Permanent?.Text ?? animeResult.Ratings?.Temporary?.Text,
            Tags = filteredTags,
            AniDbEpisodes = animeResult.Episodes.Select(e => new AniDbEpisode
            {
                AniDbAnimeId = animeResult.Id,
                Id = e.Id,
                DurationMinutes = e.Length,
                Number = EpisodeTypeExtensions.ParseEpString(e.Epno.Text)
                    .number,
                EpisodeType = e.Epno.Type,
                AirDate = string.IsNullOrEmpty(e.Airdate)
                    ? null
                    : DateTime.Parse(e.Airdate,
                        styles: DateTimeStyles.AssumeUniversal),
                Summary = e.Summary,
                Updated = DateTime.UtcNow,
                TitleEnglish = e.Title.First(t => t.Lang == "en")
                    .Text,
                TitleTranscription = e.Title.FirstOrDefault(t => t.Lang.StartsWith("x-") && t.Lang == mainTitle.Lang)
                    ?.Text,
                TitleOriginal = e.Title.FirstOrDefault(t => t.Lang.StartsWith(originalLangPrefix, StringComparison.OrdinalIgnoreCase))
                    ?.Text,
            }).ToList(),
            Updated = DateTime.UtcNow
        };
        return newAniDbAnime;
    }


    protected override async Task ProcessInnerAsync()
    {
        _logger.LogInformation("Updating data for anime id: {AnimeId}", CommandArgs.AnimeId);

        var animeResult = await GetAnimeAsync().ConfigureAwait(false);

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
        {
            if (_context.AniDbEpisodes.Find(ep.Id) is { } eEp)
                _context.Entry(eEp).CurrentValues.SetValues(ep);
            else
                _context.AniDbEpisodes.Add(ep);
            foreach (var eHangingXref in _context.HangingEpisodeFileXrefs.Where(x => x.AniDbEpisodeId == ep.Id))
            {
                if (!_context.AniDbEpisodeFileXrefs.Any(x => x.AniDbEpisodeId == ep.Id && x.AniDbFileId == eHangingXref.AniDbNormalFileId))
                    _context.AniDbEpisodeFileXrefs.Add(new AniDbEpisodeFileXref
                    {
                        AniDbEpisodeId = ep.Id,
                        AniDbFileId = eHangingXref.AniDbNormalFileId
                    });
                _context.HangingEpisodeFileXrefs.Remove(eHangingXref);
            }
        }

        _context.SaveChanges();

        if (aniDbAnime.ImageFilename is not null)
            _imageService.GetAnimePoster(aniDbAnime.Id);

        await UpdateMalXrefsAsync(animeResult).ConfigureAwait(false);

        UpdateRelatedAnime(animeResult);

        _collectionViewService.Update();
        Completed = true;
    }

    private void UpdateRelatedAnime(AnimeResult animeResult)
    {
        var eAnimeRelations = _context.AniDbAnimeRelations.Where(r => r.AnimeId == animeResult.Id).ToList();
        var animeRelations = animeResult.Relatedanime.Select(r => new AniDbAnimeRelation
        {
            AnimeId = animeResult.Id,
            ToAnimeId = r.Id,
            RelationType = Enum.Parse<RelatedAnimeType>(r.Type.WithoutSpaces(), true)
        }).ToList();
        Func<AniDbAnimeRelation, (int AnimeId, int ToAnimeId, RelatedAnimeType RelationType)> toTuple = r => (r.AnimeId, r.ToAnimeId, r.RelationType);
        _context.AniDbAnimeRelations.RemoveRange(eAnimeRelations.ExceptBy(animeRelations.Select(toTuple), toTuple));
        _context.AniDbAnimeRelations.AddRange(animeRelations.ExceptBy(eAnimeRelations.Select(toTuple), toTuple));
        _context.SaveChanges();
        var fetchDepth = CommandArgs.FetchRelationDepth ?? _options.AniDb.FetchRelationDepth;
        if (fetchDepth > 0)
        {
            var missingAnime = _context.AniDbAnimeRelations.Where(r => r.AnimeId == animeResult.Id && !_context.AniDbAnimes.Any(a => a.Id == r.ToAnimeId))
                .Select(r => r.ToAnimeId).ToList();
            _commandService.DispatchRange(missingAnime.Select(aid => new AnimeArgs(aid, fetchDepth - 1)));
        }
    }

    private async Task<AnimeResult?> GetAnimeAsync()
    {
        var timer = _context.Timers.FirstOrDefault(t => t.Type == TimerType.AnimeRequest && t.ExtraId == CommandArgs.AnimeId);
        if (timer is not null && timer.Expires > DateTime.UtcNow)
        {
            _logger.LogWarning("Anime {AnimeId} already requested recently, trying cache for HTTP anime request", CommandArgs.AnimeId);
            var serializer = new XmlSerializer(typeof(AnimeResult));
            var stream = File.OpenRead(FilePaths.HttpCachePath(CommandArgs.AnimeId));
            await using var _ = stream.ConfigureAwait(false);
            using var reader = XmlReader.Create(stream);
            return serializer.Deserialize(reader) as AnimeResult;
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

        _context.SaveChanges();
        _logger.LogInformation("Getting Anime {AnimeId} from HTTP anime request", CommandArgs.AnimeId);
        _animeRequest.SetParameters(CommandArgs.AnimeId);
        await _animeRequest.ProcessAsync().ConfigureAwait(false);
        if (_animeRequest.AnimeResult is not null)
        {
            var sw = new StreamWriter(FilePaths.HttpCachePath(CommandArgs.AnimeId));
            await using var _ = sw.ConfigureAwait(false);
            await sw.WriteAsync(_animeRequest.ResponseText).ConfigureAwait(false);
        }
        else
        {
            _logger.LogWarning("Failed to get HTTP anime data, retry in {Hours} hours", rateLimit.TotalHours);
        }

        return _animeRequest.AnimeResult;
    }

    private async Task UpdateMalXrefsAsync(AnimeResult animeResult)
    {
        var xrefs = animeResult.Resources.Where(r => (ResourceType)r.Type == ResourceType.Mal)
            .SelectMany(r => r.ExternalEntities.SelectMany(e => e.Identifiers)
                .Select(id => new MalAniDbXref { AniDbAnimeId = CommandArgs.AnimeId, MalAnimeId = int.Parse(id) })).ToList();
        var eMalIds = _context.MalAnimes.Select(a => a.Id).ToHashSet();
        foreach (var xref in xrefs.ExceptBy(eMalIds, x => x.MalAnimeId))
            await _myAnimeListService.GetAnimeAsync(xref.MalAnimeId).ConfigureAwait(false);
        eMalIds = _context.MalAnimes.Select(a => a.Id).ToHashSet();
        var eXrefs = _context.MalAniDbXrefs.Where(xref => xref.AniDbAnimeId == CommandArgs.AnimeId).ToList();
        _context.RemoveRange(eXrefs.ExceptBy(xrefs.Select(x => x.MalAnimeId), x => x.MalAnimeId));
        _context.AddRange(xrefs.ExceptBy(eXrefs.Select(x => x.MalAnimeId), x => x.MalAnimeId)
            .Where(x => eMalIds.Contains(x.MalAnimeId)));


        _context.SaveChanges();
    }
}
