using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.CommandProcessors;
using Shizou.Options;

namespace Shizou.Commands.AniDb
{
    public record HttpMyListParams() : CommandParams($"{nameof(HttpMyListCommand)}");

    [Command(CommandType.HttpGetMyList, CommandPriority.Default, QueueType.AniDbHttp)]
    public class HttpMyListCommand : BaseCommand<HttpMyListParams>
    {
        private readonly AniDbHttpProcessor _processor;
        private readonly string _url;
        private readonly string _mylistPath;


        public HttpMyListCommand(IServiceProvider provider, HttpMyListParams commandParams) : base(provider,
            provider.GetRequiredService<ILogger<HttpMyListCommand>>(), commandParams)
        {
            var options = provider.GetRequiredService<IOptions<ShizouOptions>>();
            _processor = provider.GetRequiredService<AniDbHttpProcessor>();
            var builder = new UriBuilder("http", options.Value.AniDb.ServerHost, options.Value.AniDb.HttpServerPort, "httpapi");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["client"] = "shizouhttp";
            query["clientver"] = "1";
            query["protover"] = "1";
            query["request"] = "mylist";
            query["user"] = options.Value.AniDb.Username;
            query["pass"] = options.Value.AniDb.Password;
            builder.Query = query.ToString();
            _url = builder.ToString();
            _mylistPath = Path.Combine(Constants.ApplicationData, "AniDbMyList.xml");
        }

        public override async Task Process()
        {
            string? result;
            Logger.LogInformation("HTTP Getting MyList from AniDb");
            HttpWebRequest request = WebRequest.CreateHttp(_url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new(stream))
            {
                result = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(result))
                {
                    Logger.LogWarning("No http response, may be banned");
                    _processor.Pause("No http response, may be banned");
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
            if (result is null)
            {
                if (!File.Exists(_mylistPath))
                {
                    if (!Directory.Exists(Constants.HttpCachePath))
                        Directory.CreateDirectory(Constants.HttpCachePath);
                    File.Create(_mylistPath).Dispose();
                }
            }
            else
            {
                await File.WriteAllTextAsync(_mylistPath, result, Encoding.UTF8);
            }
            Completed = true;
        }
    }
}
