using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;

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
                MyListEntryResult = new MyListEntryResult(int.Parse(data[0]),
                    int.Parse(data[1]),
                    int.Parse(data[2]),
                    int.Parse(data[3]),
                    int.Parse(data[4]),
                    DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[5])),
                    Enum.Parse<MyListState>(data[6]),
                    DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[7])),
                    data[8],
                    data[9],
                    data[10],
                    Enum.Parse<MyListFileState>(data[11]));
                break;
            case AniDbResponseCode.MultipleMyListEntries:
                break;
            case AniDbResponseCode.NoSuchEntry:
                break;
        }
        return Task.CompletedTask;
    }
}
