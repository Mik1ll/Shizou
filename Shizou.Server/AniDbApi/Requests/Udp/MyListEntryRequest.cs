using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListEntryRequest : AniDbUdpRequest
{
    public MyListEntryRequest(ILogger<MyListEntryRequest> logger, AniDbUdpState aniDbUdpState) : base("MYLIST", logger, aniDbUdpState)
    {
    }

    protected override Task HandleResponse()
    {
        throw new NotImplementedException();
    }
}
