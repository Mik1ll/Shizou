using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp.Notify;

public record NotifyListItem(string Type, int Id);

public class NotifyListRequest : AniDbUdpRequest
{
    public List<NotifyListItem>? Result { get; set; }

    public NotifyListRequest(ILogger<NotifyListRequest> logger,
        AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("NOTIFYLIST", logger, aniDbUdpState, rateLimiter)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.NotifyList:
                if (ResponseText is not null)
                    Result = new List<NotifyListItem>(ResponseText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(t =>
                    {
                        var r = t.Split('|');
                        return new NotifyListItem(r[0], int.Parse(r[1]));
                    }));
                break;
        }
        return Task.CompletedTask;
    }
}
