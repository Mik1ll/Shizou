using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListEntryRequest : AniDbUdpRequest
{
    public MyListEntryResult? MyListEntryResult { get; private set; }

    public MyListEntryRequest(ILogger<MyListEntryRequest> logger, AniDbUdpState aniDbUdpState) : base("MYLIST", logger, aniDbUdpState)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.MyList:
                if (string.IsNullOrWhiteSpace(ResponseText))
                    return Task.CompletedTask;
                var data = ResponseText.Split('|');
                MyListEntryResult = new MyListEntryResult(data);
                break;
            case AniDbResponseCode.MultipleMyListEntries:
                break;
            case AniDbResponseCode.NoSuchEntry:
                break;
        }
        return Task.CompletedTask;
    }
}
