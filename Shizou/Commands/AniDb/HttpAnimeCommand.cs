using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;
using Shizou.Database;
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
        private readonly string _url;

        public HttpAnimeCommand(IServiceProvider provider, HttpAnimeParams commandParams) : base(provider,
            provider.GetRequiredService<ILogger<HttpAnimeCommand>>(), commandParams)
        {
            var options = provider.GetRequiredService<IOptions<ShizouOptions>>();
            _processor = provider.GetRequiredService<AniDbHttpProcessor>();
            _context = provider.GetRequiredService<ShizouContext>();
            var builder = new UriBuilder("http", options.Value.AniDb.ServerHost, options.Value.AniDb.HttpServerPort, "httpapi");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["request"] = "anime";
            query["aid"] = commandParams.AnimeId.ToString();
            builder.Query = query.ToString();
            _url = builder.ToString();
            _cacheFilePath = Path.Combine(Program.HttpCachePath, $"AnimeDoc_{CommandParams.AnimeId}.xml");
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
            var animeResult = serializer.Deserialize(new StringReader(result)) as HttpAnimeResult;
            if (animeResult is null)
            {
                Completed = true;
                return;
            }

            using (var trans = _context.Database.BeginTransaction())
            {
                var animeExists = _context.AniDbAnimes.Any(e => e.Id == CommandParams.AnimeId);
                var newAniDbAnime = animeResult.ToAniDbAnime();
                if (!animeExists)
                {
                    _context.AniDbAnimes.Add(newAniDbAnime);
                }
                else
                {
                    _context.Update(newAniDbAnime);
                    foreach (var ep in newAniDbAnime.AniDbEpisodes)
                    {
                        var epExists = _context.AniDbEpisodes.Any(e => e.Id == ep.Id);
                        _context.Entry(ep).State = epExists ? EntityState.Modified : EntityState.Added;
                    }
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
            string? result;
            Logger.LogInformation("HTTP Getting anime id {animeId}", CommandParams.AnimeId);
            HttpWebRequest request = WebRequest.CreateHttp(_url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new(stream))
            {
                result = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(result))
                {
                    Logger.LogWarning("No http response, may be banned or no such anime {animeId}", CommandParams.AnimeId);
                    _processor.Paused = true;
                    _processor.PauseReason = $"No http response, may be banned or no such anime {CommandParams.AnimeId}";
                    result = null;
                }
                else
                {
                    const string errStt = "<error>";
                    const string errEnd = "</error>";
                    if (result.StartsWith(errStt))
                    {
                        var errText = result.Substring(errStt.Length, result.Length - errEnd.Length - errStt.Length);
                        if (errText == "Banned")
                        {
                            _processor.Banned = true;
                            _processor.PauseReason = $"No http response, may be banned or no such anime {CommandParams.AnimeId}";
                            Logger.LogWarning("HTTP Banned! waiting {banPeriod}", _processor.BanPeriod);
                        }
                        else
                        {
                            Logger.LogCritical("Unknown error http response, not requesting again: {errText}", errText);
                            _processor.Paused = true;
                            _processor.PauseReason = "Unknown error http response, check log";
                            Completed = true;
                        }
                        result = null;
                    }
                }
            }
            if (result is null)
            {
                if (!File.Exists(_cacheFilePath))
                {
                    if (!Directory.Exists(Program.HttpCachePath))
                        Directory.CreateDirectory(Program.HttpCachePath);
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
