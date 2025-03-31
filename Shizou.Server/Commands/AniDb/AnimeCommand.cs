using System;
using System.Collections.Generic;
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
    private readonly ShizouOptions _options;

    public AnimeCommand(
        ILogger<AnimeCommand> logger,
        IShizouContext context,
        ImageService imageService,
        MyAnimeListService myAnimeListService,
        IAnimeRequest animeRequest,
        CommandService commandService,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot)
    {
        _logger = logger;
        _context = context;
        _imageService = imageService;
        _myAnimeListService = myAnimeListService;
        _animeRequest = animeRequest;
        _commandService = commandService;
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
        }).Select(t => t.Name.Replace('`', '\'')).ToList();
        var newAniDbAnime = new AniDbAnime
        {
            Id = animeResult.Id,
            Description = animeResult.Description?.Replace('`', '\''),
            Restricted = animeResult.Restricted,
            AirDate = string.IsNullOrWhiteSpace(animeResult.Startdate)
                ? null
                : DateOnly.Parse(animeResult.Startdate switch
                {
                    { Length: 7 } => animeResult.Startdate + "-01",
                    { Length: 4 } => animeResult.Startdate + "-01-01",
                    { Length: 10 } => animeResult.Startdate,
                    _ => throw new InvalidOperationException($"Could not parse start date, unexpected length: {animeResult.Startdate.Length}")
                }),
            EndDate = string.IsNullOrWhiteSpace(animeResult.Enddate)
                ? null
                : DateOnly.Parse(animeResult.Enddate switch
                {
                    { Length: 7 } => animeResult.Enddate +
                                     $"-{DateTime.DaysInMonth(int.Parse(animeResult.Enddate[..4]), int.Parse(animeResult.Enddate[5..7]))}",
                    { Length: 4 } => animeResult.Enddate + $"-12-{DateTime.DaysInMonth(int.Parse(animeResult.Enddate[..4]), 12)}",
                    { Length: 10 } => animeResult.Enddate,
                    _ => throw new InvalidOperationException($"Could not parse end date, unexpected length: {animeResult.Enddate.Length}")
                }),
            AnimeType = animeResult.Type,
            EpisodeCount = animeResult.Episodecount == 0 ? null : animeResult.Episodecount,
            ImageFilename = animeResult.Picture,
            TitleTranscription = mainTitle.Text.Replace('`', '\''),
            TitleOriginal = animeResult.Titles
                .FirstOrDefault(t => t.Type == "official" && t.Lang.StartsWith(originalLangPrefix, StringComparison.OrdinalIgnoreCase))
                ?.Text.Replace('`', '\''),
            TitleEnglish = animeResult.Titles.FirstOrDefault(t => t is { Type: "official", Lang: "en" })?.Text.Replace('`', '\''),
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
                AirDate = string.IsNullOrWhiteSpace(e.Airdate)
                    ? null
                    : DateOnly.Parse(e.Airdate),
                Summary = e.Summary?.Replace('`', '\''),
                Updated = DateTime.UtcNow,
                TitleEnglish = e.Title.First(t => t.Lang == "en")
                    .Text.Replace('`', '\''),
                TitleTranscription = e.Title.FirstOrDefault(t => t.Lang.StartsWith("x-") && t.Lang == mainTitle.Lang)
                    ?.Text.Replace('`', '\''),
                TitleOriginal = e.Title.FirstOrDefault(t => t.Lang.StartsWith(originalLangPrefix, StringComparison.OrdinalIgnoreCase))
                    ?.Text.Replace('`', '\''),
            }).ToList(),
            Updated = DateTime.UtcNow,
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

        UpdateCredits(animeResult);

        _commandService.Dispatch(new UpdateSymbolicCollectionArgs());
        Completed = true;
    }

    private void UpdateCredits(AnimeResult animeResult)
    {
        // Just delete all credits for anime and recreate
        _context.AniDbCredits.Where(c => c.AniDbAnimeId == animeResult.Id).ExecuteDelete();

        var creatorsToUpdate = new Dictionary<int, AniDbCreator>();
        var charactersToUpdate = new Dictionary<int, AniDbCharacter>();
        var creditsToAdd = new List<AniDbCredit>();
        var creditId = 0;

        foreach (var character in animeResult.Characters)
        {
            if (!charactersToUpdate.ContainsKey(character.Id))
                charactersToUpdate[character.Id] = new AniDbCharacter()
                {
                    Id = character.Id,
                    Name = character.Name.Replace('`', '\''),
                    Type = (CharacterType)character.Charactertype.Id,
                    ImageFilename = character.Picture,
                };

            foreach (var seiyuu in character.Seiyuu)
            {
                if (!creatorsToUpdate.ContainsKey(seiyuu.Id))
                {
                    creatorsToUpdate[seiyuu.Id] = new AniDbCreator()
                    {
                        Id = seiyuu.Id,
                        Name = seiyuu.Text.Replace('`', '\''),
                        Type = CreatorType.Person,
                        ImageFilename = seiyuu.Picture,
                    };
                }

                creditsToAdd.Add(new AniDbCredit()
                {
                    Id = ++creditId,
                    AniDbAnimeId = animeResult.Id,
                    Role = character.Type,
                    AniDbCreatorId = seiyuu.Id,
                    AniDbCharacterId = character.Id,
                });
            }
        }

        foreach (var creatorResult in animeResult.Creators)
        {
            if (!creatorsToUpdate.ContainsKey(creatorResult.Id))
                creatorsToUpdate[creatorResult.Id] = new AniDbCreator()
                {
                    Id = creatorResult.Id,
                    Name = creatorResult.Text.Replace('`', '\''),
                    Type = CreatorType.Unknown,
                    ImageFilename = null,
                };

            creditsToAdd.Add(new AniDbCredit()
            {
                Id = ++creditId,
                AniDbAnimeId = animeResult.Id,
                Role = creatorResult.Type,
                AniDbCreatorId = creatorResult.Id,
            });
        }

        // Ensure creators and characters are updated before credits
        UpdateCreators(creatorsToUpdate);
        UpdateCharacters(charactersToUpdate);

        _context.AniDbCredits.AddRange(creditsToAdd);
        _context.SaveChanges();
    }

    private void UpdateCharacters(Dictionary<int, AniDbCharacter> characters)
    {
        var characterIds = characters.Keys.ToArray();
        var eCharacters = _context.AniDbCharacters.Where(ch => characterIds.Contains(ch.Id)).ToDictionary(ch => ch.Id);
        foreach (var character in characters.Values)
            if (eCharacters.TryGetValue(character.Id, out var eCharacter))
                _context.Entry(eCharacter).CurrentValues.SetValues(character);
            else
                _context.AniDbCharacters.Add(character);
        _context.SaveChanges();
    }

    private void UpdateCreators(Dictionary<int, AniDbCreator> creators)
    {
        var creatorIds = creators.Keys.ToArray();
        var eCreators = _context.AniDbCreators.Where(c => creatorIds.Contains(c.Id)).ToDictionary(c => c.Id);
        var getImageForCreatorIds = new HashSet<int>();
        var needMoreInfoOnCreatorIds = new HashSet<int>();
        foreach (var creator in creators.Values)
        {
            if (eCreators.TryGetValue(creator.Id, out var eCreator))
            {
                eCreator.Name = creator.Name;
                if (creator.Type is not CreatorType.Unknown)
                    eCreator.Type = creator.Type;
                if (!string.IsNullOrWhiteSpace(creator.ImageFilename))
                    eCreator.ImageFilename = creator.ImageFilename;
            }
            else
            {
                _context.AniDbCreators.Add(creator);
            }

            if (eCreator?.Type is CreatorType.Unknown || creator.Type is CreatorType.Unknown)
                needMoreInfoOnCreatorIds.Add(creator.Id);
            // If we queue a get creator command, we will retrieve the image if it doesn't exist, so no need to do it now
            else if (!string.IsNullOrWhiteSpace(creator.ImageFilename) && !File.Exists(FilePaths.CreatorImagePath(creator.ImageFilename)))
                getImageForCreatorIds.Add(creator.Id);
        }

        _context.SaveChanges();

        foreach (var cid in getImageForCreatorIds)
            _imageService.GetCreatorImage(cid);
        _commandService.DispatchRange(needMoreInfoOnCreatorIds.Select(id => new CreatorArgs(id)));
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
            Directory.CreateDirectory(FilePaths.HttpCacheDir);
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
