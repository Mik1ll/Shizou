using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public sealed record NoopArgs(int Testint) : CommandArgs($"Noop_{Testint}", CommandPriority.Normal, QueueType.General);
