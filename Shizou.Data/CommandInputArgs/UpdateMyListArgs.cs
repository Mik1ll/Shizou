using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record UpdateMyListArgs(
    int Lid,
    MyListState MyListState,
    bool? Watched,
    DateTimeOffset? WatchedDate,
    int FileId)
    : CommandArgs($"UpdateMyList_lid={Lid}_watched={Watched}_watchedDate={WatchedDate}_state={MyListState}_uid={Path.GetRandomFileName()[..8]}",
        CommandPriority.Normal, QueueType.AniDbUdp);
