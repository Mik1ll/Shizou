using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public record MyListEntryResult(int MyListId, int FileId, int EpisodeId, int AnimeId, int GroupId, DateTimeOffset AddedDate, MyListState State,
    DateTimeOffset ViewedDate, string Storage, string Source, string Other, MyListFileState FileState);
