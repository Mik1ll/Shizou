using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public record MyListEntryResult(int MyListId, int FileId, int EpisodeId, int AnimeId, int GroupId, DateTimeOffset AddedDate, MyListState State,
    DateTimeOffset? ViewDate, string Storage, string Source, string Other, MyListFileState FileState)
{
    public MyListEntryResult(string[] data) : this(int.Parse(data[0]),
        int.Parse(data[1]),
        int.Parse(data[2]),
        int.Parse(data[3]),
        int.Parse(data[4]),
        DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[5])),
        Enum.Parse<MyListState>(data[6]),
        data[7] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[7])) : null,
        data[8],
        data[9],
        data[10],
        Enum.Parse<MyListFileState>(data[11]))
    {
    }
}
