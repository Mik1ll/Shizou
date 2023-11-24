using Shizou.Data.Enums;

namespace Shizou.Data.CommandArgs;

public record SyncMyListArgs() : CommandArgs("SyncMyList", CommandPriority.Low, QueueType.AniDbHttp);
