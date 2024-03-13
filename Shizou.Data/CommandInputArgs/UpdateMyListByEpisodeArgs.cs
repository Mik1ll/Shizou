using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record UpdateMyListByEpisodeArgs(
    bool Edit,
    int Aid,
    string EpNo,
    MyListState? MyListState = null,
    bool? Watched = null,
    DateTimeOffset? WatchedDate = null,
    int? ManualLinkToLocalFileId = null)
    : CommandArgs(
        $"UpdateMyListGeneric_aid={Aid}_epno={EpNo}_edit={Edit}_watched={Watched}_watchedDate={WatchedDate}_state={MyListState}_uid={Path.GetRandomFileName()[..8]}",
        CommandPriority.Normal, QueueType.AniDbUdp);
