using System;
using Shizou.Enums;

namespace Shizou.AniDbApi.Results;

public record AniDbMyListAddResult(int? ListId,
    MyListState? State,
    bool? Watched,
    DateTime? WatchedDate,
    MyListFileState? FileState);
