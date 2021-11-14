﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
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
            _processor = (AniDbHttpProcessor)provider.GetServices<CommandProcessor>().First(p => p.QueueType == QueueType.AniDbHttp);
            _context = provider.GetRequiredService<ShizouContext>();
            var builder = new UriBuilder("http", options.Value.AniDb.ServerHost, options.Value.AniDb.HttpServerPort, "httpapi");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["client"] = "shizouhttp";
            query["clientver"] = "1";
            query["protover"] = "1";
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

            var newAniDbAnime = animeResult.ToAniDbAnime();
            using (var trans = _context.Database.BeginTransaction())
            {
                var aniDbAnime = _context.AniDbAnimes.Find(CommandParams.AnimeId);
                if (aniDbAnime is null)
                {
                    _context.AniDbAnimes.Add(newAniDbAnime);
                }
                else
                {
                    _context.Entry(aniDbAnime).CurrentValues.SetValues(newAniDbAnime);
                    foreach (var newEp in newAniDbAnime.AniDbEpisodes)
                    {
                        var ep = _context.AniDbEpisodes.Find(newEp.Id);
                        if (ep is null)
                            _context.AniDbEpisodes.Add(newEp);
                        else
                            _context.Entry(ep).CurrentValues.SetValues(newEp);
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
                    const string errStt = "<error";
                    if (result.StartsWith(errStt))
                    {
                        if (result.Contains("Banned"))
                        {
                            _processor.Banned = true;
                            _processor.PauseReason = $"No http response, may be banned or no such anime {CommandParams.AnimeId}";
                            Logger.LogWarning("HTTP Banned! waiting {banPeriod}", _processor.BanPeriod);
                        }
                        else
                        {
                            Logger.LogCritical("Unknown error http response, not requesting again: {errText}", result);
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
