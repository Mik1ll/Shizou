using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Options;

namespace Shizou.Commands.AniDb
{
    public record HttpAnimeParams(int AnimeId) : CommandParams;

    public class HttpAnimeCommand : BaseCommand<HttpAnimeParams>
    {
        private readonly ShizouContext _context;
        private readonly AniDbHttpProcessor _processor;
        private readonly string _url;

        public HttpAnimeCommand(IServiceProvider provider, HttpAnimeParams commandParams) : base(provider,
            provider.GetRequiredService<ILogger<HttpAnimeCommand>>(), commandParams)
        {
            var options = provider.GetRequiredService<ShizouOptions>();
            _processor = provider.GetRequiredService<AniDbHttpProcessor>();
            _context = provider.GetRequiredService<ShizouContext>();
            var builder = new UriBuilder("http", options.AniDb.ServerHost, options.AniDb.HttpServerPort, "httpapi");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["request"] = "anime";
            query["aid"] = commandParams.AnimeId.ToString();
            builder.Query = query.ToString();
            _url = builder.ToString();
            CommandId = $"{nameof(HttpAnimeCommand)}_{commandParams.AnimeId}";
        }

        public override string CommandId { get; }

        public override async Task Process()
        {
            var aniDbAnime = _context.AniDbAnimes.Find(CommandParams.AnimeId);
            if (aniDbAnime is not null)
            {
                if (aniDbAnime.Updated.HasValue && DateTime.UtcNow - aniDbAnime.Updated < TimeSpan.FromHours(24))
                {
                    Logger.LogWarning("Ignoring HTTP anime request: {animeId}, already requested in last 24 hours", CommandParams.AnimeId);
                    Completed = true;
                    return;
                }
                aniDbAnime.Updated = DateTime.UtcNow;
            }
            Logger.LogInformation("HTTP Getting anime id {animeId}", CommandParams.AnimeId);
            HttpWebRequest request = WebRequest.CreateHttp(_url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new(stream))
            {
                string result = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(result))
                {
                    Logger.LogWarning("No http response, may be banned or no such anime {animeId}", CommandParams.AnimeId);
                    _processor.Paused = true;
                    _processor.PauseReason = $"No http response, may be banned or no such anime {CommandParams.AnimeId}";
                }
                const string errStt = "<error>";
                const string errEnd = "</error>";
                if (result.StartsWith(errStt))
                {
                    var errText = result.Substring(errStt.Length, result.Length - errEnd.Length - errStt.Length);
                    if (errText == "Banned")
                    {
                        _processor.Banned = true;
                        _processor.PauseReason = $"No http response, may be banned or no such anime {CommandParams.AnimeId}";
                        return;
                    }
                    Logger.LogCritical("Unknown error http response, not requesting again: {errText}", errText);
                    Completed = true;
                    return;
                }
            }
            Completed = true;
        }
    }
}
