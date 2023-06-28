using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp.Results;

public record AniDbMyListAddResult(int? ListId,
    DateTimeOffset Updated,
    MyListState? State,
    bool? Watched,
    DateTimeOffset? WatchedDate,
    MyListFileState? FileState);
