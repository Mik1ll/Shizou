﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public record MessageResult(int Id, int FromUserId, string UserName, DateTimeOffset Date, MessageType Type, string Title, string Body);

public class MessageGetRequest : AniDbUdpRequest
{
    public MessageResult? Result { get; set; }

    public MessageGetRequest(
        ILogger<MessageGetRequest> logger,
        AniDbUdpState aniDbUdpState
    ) : base("NOTIFYGET", logger, aniDbUdpState)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.MessageGet:
                if (ResponseText is not null)
                {
                    var data = ResponseText.TrimEnd().Split('|');
                    Result = new MessageResult(int.Parse(data[0]), int.Parse(data[1]), data[2],
                        DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[3])), Enum.Parse<MessageType>(data[4]), data[5], data[6].Replace("<br />", "\n"));
                }
                break;
            case AniDbResponseCode.NoSuchMessageEntry:
                break;
        }
        return Task.CompletedTask;
    }
}
