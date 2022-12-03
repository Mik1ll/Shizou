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
using Microsoft.Extensions.Options;
using Shizou.AniDbApi.Results;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Models;
using Shizou.Options;

namespace Shizou.Commands.AniDb
{
    public record HttpAnimeParams(int AnimeId, bool ForceRefresh = false) : CommandParams($"{nameof(HttpAnimeCommand)}_{AnimeId}");

    [Command(CommandType.HttpGetAnime, CommandPriority.Default, QueueType.AniDbHttp)]
    public class HttpAnimeCommand : BaseCommand<HttpAnimeParams>
    {
        private readonly string _cacheFilePath;
        private readonly ShizouContext _context;
        private readonly AniDbHttpProcessor _processor;
        private readonly HttpClient _httpClient;
        private readonly string _url;

        public HttpAnimeCommand(IServiceProvider provider, HttpAnimeParams commandParams) : base(provider,
            provider.GetRequiredService<ILogger<HttpAnimeCommand>>(), commandParams)
        {
            var options = provider.GetRequiredService<IOptions<ShizouOptions>>();
            _processor = provider.GetRequiredService<AniDbHttpProcessor>();
            _context = provider.GetRequiredService<ShizouContext>();
            _httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("gzip");
            var builder = new UriBuilder("http", options.Value.AniDb.ServerHost, options.Value.AniDb.HttpServerPort, "httpapi");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["client"] = "shizouhttp";
            query["clientver"] = "1";
            query["protover"] = "1";
            query["request"] = "anime";
            query["aid"] = commandParams.AnimeId.ToString();
            builder.Query = query.ToString();
            _url = builder.ToString();
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

            using (var trans = _context.Database.BeginTransaction())
            {
                var aniDbAnime = _context.AniDbAnimes.Include(a => a.AniDbEpisodes)
                    .FirstOrDefault(a => a.Id == CommandParams.AnimeId);
                var newAniDbAnime = new AniDbAnime(animeResult);
                if (aniDbAnime is null)
                    _context.AniDbAnimes.Add(newAniDbAnime);
                else
                {
                    _context.Entry(aniDbAnime).CurrentValues.SetValues(newAniDbAnime);
                    _context.ReplaceNavigationList(newAniDbAnime.AniDbEpisodes, aniDbAnime.AniDbEpisodes);
                }

                _context.SaveChanges();
                trans.Commit();
            }
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
            string? result = null;
            Logger.LogInformation("HTTP Getting anime id {animeId}", CommandParams.AnimeId);
            try
            {
                result = HttpUtility.HtmlDecode(await _httpClient.GetStringAsync(_url));
                if (string.IsNullOrWhiteSpace(result))
                {
                    Logger.LogWarning("No http response, may be banned or no such anime {animeId}", CommandParams.AnimeId);
                    _processor.Pause($"No http response, may be banned or no such anime {CommandParams.AnimeId}");
                    result = null;
                }
                else
                {
                    const string errStt = "<error";
                    if (result.StartsWith(errStt))
                    {
                        if (result.Contains("Banned"))
                        {
                            _processor.Banned = true;
                            _processor.Pause($"HTTP Banned, wait {_processor.BanPeriod}");
                            Logger.LogWarning("HTTP Banned! waiting {banPeriod}", _processor.BanPeriod);
                        }
                        else
                        {
                            Logger.LogCritical("Unknown error http response, not requesting again: {errText}", result);
                            _processor.Pause("Unknown error http response, check log");
                            Completed = true;
                        }
                        result = null;
                    }
                }
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
}
