using Shizou.Data.Enums;

namespace Shizou.Data.CommandArgs;

public sealed record AddMissingMyListEntriesArgs() : CommandArgs("AddMissingMyList", CommandPriority.Low, QueueType.AniDbUdp);
