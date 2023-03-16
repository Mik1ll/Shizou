using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi.Requests.Http;

public class AnimeRequest : HttpRequest
{
    public AnimeRequest(IServiceProvider provider, int aid) : base(provider)
    {
        Params["request"] = "anime";
        Params["aid"] = aid.ToString();
    }

    public override async Task Process()
    {
        Logger.LogInformation("HTTP Getting anime from AniDb");
        await SendRequest();
    }
}
