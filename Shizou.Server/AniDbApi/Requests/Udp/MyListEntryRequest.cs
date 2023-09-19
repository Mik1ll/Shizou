using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListEntryRequest : AniDbUdpRequest, IMyListEntryRequest
{
    public MyListEntryResult? MyListEntryResult { get; private set; }

    public MyListEntryRequest(ILogger<MyListEntryRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("MYLIST", logger,
        aniDbUdpState, rateLimiter)
    {
    }


    public void SetParameters(int aid, string epno)
    {
        Args["aid"] = aid.ToString();
        Args["epno"] = epno;
        ParametersSet = true;
    }

    public void SetParameters(int id, IdTypeFileMyList idType)
    {
        Args[idType switch
        {
            IdTypeFileMyList.FileId => "fid",
            IdTypeFileMyList.MyListId => "lid",
            _ => throw new ArgumentOutOfRangeException(nameof(idType), idType, null)
        }] = id.ToString();
        ParametersSet = true;
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