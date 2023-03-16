using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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

public record HttpAnimeParams(int AnimeId, bool ForceRefresh = false) : CommandParams($"{nameof(HttpAnimeCommand)}_{AnimeId}");

[Command(CommandType.HttpGetAnime, CommandPriority.Default, QueueType.AniDbHttp)]
public class HttpAnimeCommand : BaseCommand<HttpAnimeParams>
{
    private readonly IServiceProvider _provider;
    private readonly string _cacheFilePath;
    private readonly ShizouContext _context;

    public HttpAnimeCommand(IServiceProvider provider, HttpAnimeParams commandParams) : base(provider, commandParams)
    {
        _provider = provider;
        _context = provider.GetRequiredService<ShizouContext>();
        _cacheFilePath = Path.Combine(Constants.HttpCachePath, $"AnimeDoc_{CommandParams.AnimeId}.xml");
    }

    public override async Task Process()
    {
        var (cacheHit, requestable) = CheckCache();
        if ((!cacheHit || CommandParams.ForceRefresh) && !requestable)
        {
            Logger.LogWarning("Ignoring HTTP anime request: {animeId}, already requested in last 24 hours", CommandParams.AnimeId);
            Completed = true;
            return;
        }
        string? result;
        if (!cacheHit || CommandParams.ForceRefresh)
            result = await GetAnimeHttp();
        else
            result = await GetAnimeCache();

        if (result is null)
        {
            Completed = true;
            return;
        }
        XmlSerializer serializer = new(typeof(HttpAnimeResult));
        var animeResult = serializer.Deserialize(XmlReader.Create(new StringReader(result))) as HttpAnimeResult;
        if (animeResult is null)
        {
            Completed = true;
            return;
        }

        var aniDbAnime = _context.AniDbAnimes.Include(a => a.AniDbEpisodes)
            .FirstOrDefault(a => a.Id == CommandParams.AnimeId);
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

    private async Task<string?> GetAnimeCache()
    {
        Logger.LogInformation("Cache getting anime id {animeId}", CommandParams.AnimeId);
        if (File.Exists(_cacheFilePath))
            return await File.ReadAllTextAsync(_cacheFilePath);
        return null;
    }

    private (bool Hit, bool canRequest) CheckCache()
    {
        var fileInfo = new FileInfo(_cacheFilePath);
        if (fileInfo.Exists)
            return (fileInfo.Length != 0, DateTime.UtcNow - fileInfo.LastWriteTimeUtc > TimeSpan.FromHours(24));
        return (false, true);
    }

    private async Task<string?> GetAnimeHttp()
    {
        var request = new AnimeRequest(_provider, CommandParams.AnimeId);
        string? result = null;
        Logger.LogInformation("HTTP Getting anime id {animeId}", CommandParams.AnimeId);
        try
        {
            await request.Process();
            if (request.Errored) return null;
            result = HttpUtility.HtmlDecode(request.ResponseText);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning("Http anime request failed: {Message}", ex.Message);
        }
        if (result is null)
        {
            if (!File.Exists(_cacheFilePath))
            {
                if (!Directory.Exists(Constants.HttpCachePath))
                    Directory.CreateDirectory(Constants.HttpCachePath);
                File.Create(_cacheFilePath).Dispose();
            }
            File.SetLastWriteTime(_cacheFilePath, DateTime.UtcNow);
        }
        else
        {
            await File.WriteAllTextAsync(_cacheFilePath, result, Encoding.UTF8);
        }
        return result;
    }
}
