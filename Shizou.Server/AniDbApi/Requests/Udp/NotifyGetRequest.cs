using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public record NotificationResult(int RelatedId, NotificationType Type, int Count, DateTimeOffset Date, string RelatedName, List<int> FileIds);

public class NotifyGetRequest : AniDbUdpRequest
{
    public NotificationResult? Result { get; set; }

    public NotifyGetRequest(
        ILogger<NotifyGetRequest> logger,
        AniDbUdpState aniDbUdpState
    ) : base("NOTIFYGET", logger, aniDbUdpState)
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
                    Result = new NotificationResult(int.Parse(data[0]), Enum.Parse<NotificationType>(data[1]), int.Parse(data[2]),
                        DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[3])), data[4], data[5].Split(',').Select(int.Parse).ToList());
                }
                break;
            case AniDbResponseCode.NoSuchNotifyEntry:
                break;
        }
        return Task.CompletedTask;
    }
}
