using Shizou.Data.Enums;

namespace Shizou.Data.CommandArgs;

public record HashArgs(string Path) : CommandArgs($"Hash_{Path}", CommandPriority.Normal, QueueType.Hash);
