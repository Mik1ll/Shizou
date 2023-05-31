using System;
using ShizouCommon.Enums;

namespace Shizou.AniDbApi.Requests.Udp.Results;

public record AniDbMyListAddResult(int? ListId,
    DateTimeOffset Updated,
    MyListState? State,
    bool? Watched,
    DateTimeOffset? WatchedDate,
    MyListFileState? FileState);
