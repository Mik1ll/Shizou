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


    public HttpMyListCommand(IServiceProvider provider, HttpMyListParams commandParams) : base(provider, commandParams)
    {
        _provider = provider;
    }

    public override async Task Process()
    {
        // TODO: Use cache first every 24 hours
        var request = new MyListRequest(_provider);
        await request.Process();
        if (request.Errored)
            return;

        var result = request.MyListResult;
        await File.WriteAllTextAsync(Constants.MyListPath, request.ResponseText, Encoding.UTF8);
        Logger.LogInformation("HTTP Get mylist succeeded");
        Completed = true;
    }
}
