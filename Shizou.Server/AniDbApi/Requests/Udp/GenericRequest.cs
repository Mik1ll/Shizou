using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class GenericRequest : AniDbUdpRequest
{
    public GenericRequest(
        ILogger<GenericRequest> logger, AniDbUdpState aniDbUdpState
    ) : base("", logger, aniDbUdpState)
    {
    }

    protected override Task HandleResponse()
    {
        return Task.CompletedTask;
    }
}
