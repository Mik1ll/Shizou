using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record AddMyListArgs(
    int Fid,
    MyListState MyListState,
    bool? Watched,
    DateTimeOffset? WatchedDate)
    : CommandArgs($"AddMyList_fid={Fid}_watched={Watched}_watchedDate={WatchedDate}_state={MyListState}_uid={Path.GetRandomFileName()[..8]}",
        CommandPriority.Normal, QueueType.AniDbUdp);
