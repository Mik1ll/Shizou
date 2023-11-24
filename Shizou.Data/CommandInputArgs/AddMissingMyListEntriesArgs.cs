using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public sealed record AddMissingMyListEntriesArgs() : CommandArgs("AddMissingMyList", CommandPriority.Low, QueueType.AniDbUdp);
