using System;
using System.Collections.Generic;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp.Notify;

public record NotifyGetResult(int RelatedId, NotificationType Type, int Count, DateTimeOffset Date, string RelatedName, List<int> FileIds);
