using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public record MyListAddResult(int? ListId,
    DateTimeOffset Added,
    MyListState? State,
    bool? Watched,
    DateTimeOffset? WatchedDate,
    MyListFileState? FileState);
