using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record UpdateMyListArgs(
    bool Edit,
    MyListState? MyListState = null,
    bool? Watched = null,
    DateTimeOffset? WatchedDate = null,
    int? Lid = null,
    int? Fid = null)
    : CommandArgs(
        $"UpdateMyList_lid={Lid}_fid={Fid}_edit={Edit}_watched={Watched}_watchedDate={WatchedDate}_state={MyListState}_uid={Path.GetRandomFileName()[..8]}",
        CommandPriority.Normal, QueueType.AniDbUdp);
