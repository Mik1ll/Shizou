using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests.Http;
using Shizou.CommandProcessors;

namespace Shizou.Commands.AniDb;

public record HttpMyListParams() : CommandParams($"{nameof(HttpMyListCommand)}");

[Command(CommandType.HttpGetMyList, CommandPriority.Default, QueueType.AniDbHttp)]
public class HttpMyListCommand : BaseCommand<HttpMyListParams>
{
    private readonly IServiceProvider _provider;

    public TimeSpan MyListRequestPeriod { get; } = TimeSpan.FromHours(24);


    public HttpMyListCommand(IServiceProvider provider, HttpMyListParams commandParams) : base(provider, commandParams)
    {
        _provider = provider;
    }

    public override async Task Process()
    {
        var (cacheHit, requestable) = CheckCache();
        if (!requestable)
        {
            if (!cacheHit)
                Logger.LogWarning("Failed to get mylist: already requested in last {hours} hours and no local cache", MyListRequestPeriod.Hours);
            else
                Logger.LogInformation("Already requested mylist in last {hours} hours, not requesting", MyListRequestPeriod.Hours);
            Completed = true;
            return;
        }

        var request = new MyListRequest(_provider);
        await request.Process();
        if (request.MyListResult is null)
        {
            if (!File.Exists(Constants.MyListPath))
                File.Create(Constants.MyListPath).Dispose();
            File.SetLastWriteTimeUtc(Constants.MyListPath, DateTime.UtcNow);
            Logger.LogWarning("Failed to get mylist data, retry in {hours} hours", MyListRequestPeriod.Hours);
            Completed = true;
            return;
        }
        await File.WriteAllTextAsync(Constants.MyListPath, request.ResponseText, Encoding.UTF8);

        Logger.LogInformation("HTTP Get mylist succeeded");
        Completed = true;
    }


    private (bool Hit, bool CanRequest) CheckCache()
    {
        var fileInfo = new FileInfo(Constants.MyListPath);
        if (fileInfo.Exists)
            return (fileInfo.Length != 0, DateTime.UtcNow - fileInfo.LastWriteTimeUtc > MyListRequestPeriod);
        return (false, true);
    }
}
