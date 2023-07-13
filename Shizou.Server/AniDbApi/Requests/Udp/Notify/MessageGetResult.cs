using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp.Notify;

public record MessageGetResult(int Id, int FromUserId, string UserName, DateTimeOffset Date, MessageType Type, string Title, string Body);
