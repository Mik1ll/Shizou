using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class GenericRequest : AniDbUdpRequest, IGenericRequest
{
    public GenericRequest(ILogger<GenericRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("", logger, aniDbUdpState,
        rateLimiter)
    {
    }

    public void SetParameters(string command, Dictionary<string, string> args)
    {
        Command = command;
        args.ToList().ForEach(a => Args.Add(a.Key, a.Value));
        ParametersSet = true;
    }

    protected override Task HandleResponse()
    {
        return Task.CompletedTask;
    }
}
