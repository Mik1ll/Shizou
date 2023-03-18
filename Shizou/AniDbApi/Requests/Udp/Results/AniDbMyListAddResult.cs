﻿using System;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests.Udp.Results;

public record AniDbMyListAddResult(int? ListId,
    MyListState? State,
    bool? Watched,
    DateTimeOffset? WatchedDate,
    MyListFileState? FileState);