using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record CreatorArgs(int CreatorId) : CommandArgs($"Creator_{CreatorId}", CommandPriority.Low, QueueType.AniDbUdp);
