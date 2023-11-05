using System;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListEntryResponse : UdpResponse
{
    public MyListEntryResult? MyListEntryResult { get; init; }
}

public class MyListEntryRequest : AniDbUdpRequest<MyListEntryResponse>, IMyListEntryRequest
{
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

    protected override MyListEntryResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        MyListEntryResult? myListEntryResult = null;
        switch (responseCode)
        {
            case AniDbResponseCode.MyList:
                if (string.IsNullOrWhiteSpace(responseText))
                    break;
                var data = responseText.Split('|');
                myListEntryResult = new MyListEntryResult(data);
                break;
            case AniDbResponseCode.MultipleMyListEntries:
                break;
            case AniDbResponseCode.NoSuchEntry:
                break;
        }

        return new MyListEntryResponse
        {
            ResponseText = responseText,
            ResponseCode = responseCode,
            ResponseCodeText = responseCodeText,
            MyListEntryResult = myListEntryResult
        };
    }
}
