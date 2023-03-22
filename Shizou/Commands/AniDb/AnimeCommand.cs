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
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Models;

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
        _cacheFilePath = Path.Combine(Constants.HttpCachePath, $"AnimeDoc_{CommandArgs.AnimeId}.xml");
    }

    public override async Task Process()
    {
        var (cacheHit, requestable) = CheckCache();
        if ((!cacheHit || CommandArgs.ForceRefresh) && !requestable)
        {
            Logger.LogWarning("Ignoring HTTP anime request: {animeId}, already requested in last {hours} hours", CommandArgs.AnimeId,
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
        var newAniDbAnime = new AniDbAnime(animeResult);
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
        Logger.LogInformation("Cache getting anime id {animeId}", CommandArgs.AnimeId);
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
                Directory.CreateDirectory(Constants.HttpCachePath);
                File.Create(_cacheFilePath).Dispose();
            }
            File.SetLastWriteTimeUtc(_cacheFilePath, DateTime.UtcNow);
            Logger.LogWarning("Failed to get HTTP anime data, retry in {hours} hours", AnimeRequestPeriod.Hours);
        }
        else
        {
            await File.WriteAllTextAsync(_cacheFilePath, request.ResponseText, Encoding.UTF8);
        }
        return request.AnimeResult;
    }
}
