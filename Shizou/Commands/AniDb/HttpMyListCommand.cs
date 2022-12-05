using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.CommandProcessors;
using Shizou.Options;

namespace Shizou.Commands.AniDb;

public record HttpMyListParams() : CommandParams($"{nameof(HttpMyListCommand)}");

[Command(CommandType.HttpGetMyList, CommandPriority.Default, QueueType.AniDbHttp)]
public class HttpMyListCommand : BaseCommand<HttpMyListParams>
{
    private readonly AniDbHttpProcessor _processor;
    private readonly string _url;
    private readonly string _mylistPath;
    private readonly HttpClient _httpClient;


    public HttpMyListCommand(IServiceProvider provider, HttpMyListParams commandParams) : base(provider,
        provider.GetRequiredService<ILogger<HttpMyListCommand>>(), commandParams)
    {
        var options = provider.GetRequiredService<IOptions<ShizouOptions>>();
        _processor = provider.GetRequiredService<AniDbHttpProcessor>();
        _httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("gzip");
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
        var retry = false;
        Logger.LogInformation("HTTP Getting mylist from AniDb");
        try
        {
            result = await _httpClient.GetStringAsync(_url);
            if (string.IsNullOrWhiteSpace(result))
            {
                Logger.LogWarning("No http response, may be banned");
                _processor.Pause("No http response, may be banned");
                retry = true;
            }
            else if (!result.StartsWith("<error"))
            {
                await File.WriteAllTextAsync(_mylistPath, result, Encoding.UTF8);
                Logger.LogInformation("HTTP Get mylist succeeded");
            }
            else if (result.Contains("Banned"))
            {
                _processor.Banned = true;
                _processor.Pause($"HTTP Banned, wait {_processor.BanPeriod}");
                Logger.LogWarning("HTTP Banned! waiting {banPeriod}", _processor.BanPeriod);
                retry = true;
            }
            else
            {
                Logger.LogCritical("Unknown error http response, not requesting again: {errText}", result);
                _processor.Pause("Unknown error http response, check log");
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning("Http mylist request failed: {Message}", ex.Message);
        }
        if (!retry)
            Completed = true;
    }
}