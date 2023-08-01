using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp.Notify;

public class NotifyGetRequest : AniDbUdpRequest
{
    public NotifyGetResult? Result { get; set; }

    public NotifyGetRequest(ILogger<NotifyGetRequest> logger,
        AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("NOTIFYGET", logger, aniDbUdpState, rateLimiter)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.NotifyGet:
                if (ResponseText is not null)
                {
                    var data = ResponseText.TrimEnd().Split('|');
                    Result = new NotifyGetResult(int.Parse(data[0]), Enum.Parse<NotificationType>(data[1]), int.Parse(data[2]),
                        DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[3])), data[4], data[5].Split(',').Select(int.Parse).ToList());
                }
                break;
            case AniDbResponseCode.NoSuchNotifyEntry:
                break;
        }
        return Task.CompletedTask;
    }
}
