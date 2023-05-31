using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests.Http;
using Shizou.AniDbApi.Requests.Http.Results;
using ShizouCommon.Enums;
using ShizouData;
using ShizouData.Database;
using ShizouData.Models;

namespace Shizou.Commands.AniDb;

public record AnimeArgs(int AnimeId, bool ForceRefresh = false) : CommandArgs($"{nameof(AnimeCommand)}_{AnimeId}_force={ForceRefresh}");

[Command(CommandType.GetAnime, CommandPriority.Normal, QueueType.AniDbHttp)]
public class AnimeCommand : BaseCommand<AnimeArgs>
{
    private readonly IServiceProvider _provider;
    private readonly string _cacheFilePath;
    private readonly ShizouContext _context;
    public TimeSpan AnimeRequestPeriod { get; } = TimeSpan.FromHours(24);

    public AnimeCommand(IServiceProvider provider, AnimeArgs commandArgs) : base(provider, commandArgs)
    {
        _provider = provider;
        _context = provider.GetRequiredService<ShizouContext>();
        _cacheFilePath = Path.Combine(FilePaths.HttpCacheDir, $"AnimeDoc_{CommandArgs.AnimeId}.xml");
    }

    public override async Task Process()
    {
        var (cacheHit, requestable) = CheckCache();
        if ((!cacheHit || CommandArgs.ForceRefresh) && !requestable)
        {
            Logger.LogWarning("Ignoring HTTP anime request: {AnimeId}, already requested in last {Hours} hours", CommandArgs.AnimeId,
                AnimeRequestPeriod.Hours);
            Completed = true;
            return;
        }
        HttpAnimeResult? animeResult;
        if (!cacheHit || CommandArgs.ForceRefresh)
            animeResult = await GetAnimeHttp();
        else
            animeResult = GetAnimeCache();

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
                AirDate = string.IsNullOrEmpty(e.Airdate) ? null : DateTimeOffset.Parse(e.Airdate + "+00:00"),
                Updated = DateTimeOffset.UtcNow,
                TitleEnglish = e.Title.First(t => t.Lang == "en").Text,
                TitleRomaji = e.Title.FirstOrDefault(t => t.Lang.StartsWith("x-") && t.Lang == mainTitle.Lang)?.Text,
                TitleKanji = e.Title.FirstOrDefault(t =>
                    t.Lang.StartsWith(mainTitle.Lang switch { "x-jat" => "ja", "x-zht" => "zh-han", "x-kot" => "ko", _ => "none" },
                        StringComparison.OrdinalIgnoreCase))?.Text
            }).ToList(),
            Updated = DateTimeOffset.UtcNow
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

    private HttpAnimeResult? GetAnimeCache()
    {
        Logger.LogInformation("Cache getting anime id {AnimeId}", CommandArgs.AnimeId);
        if (File.Exists(_cacheFilePath))
        {
            XmlSerializer serializer = new(typeof(HttpAnimeResult));
            using var reader = XmlReader.Create(_cacheFilePath);
            return serializer.Deserialize(reader) as HttpAnimeResult;
        }
        return null;
    }

    private (bool Hit, bool CanRequest) CheckCache()
    {
        var fileInfo = new FileInfo(_cacheFilePath);
        if (fileInfo.Exists)
            return (fileInfo.Length != 0, DateTime.UtcNow - fileInfo.LastWriteTimeUtc > AnimeRequestPeriod);
        return (false, true);
    }

    private async Task<HttpAnimeResult?> GetAnimeHttp()
    {
        var request = new AnimeRequest(_provider, CommandArgs.AnimeId);
        await request.Process();
        if (request.AnimeResult is null)
        {
            if (!File.Exists(_cacheFilePath))
            {
                Directory.CreateDirectory(FilePaths.HttpCacheDir);
                File.Create(_cacheFilePath).Dispose();
            }
            File.SetLastWriteTimeUtc(_cacheFilePath, DateTime.UtcNow);
            Logger.LogWarning("Failed to get HTTP anime data, retry in {Hours} hours", AnimeRequestPeriod.Hours);
        }
        else
        {
            await File.WriteAllTextAsync(_cacheFilePath, request.ResponseText, Encoding.UTF8);
        }
        return request.AnimeResult;
    }
}
