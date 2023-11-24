using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record SyncMyListArgs() : CommandArgs("SyncMyList", CommandPriority.Low, QueueType.AniDbHttp);
