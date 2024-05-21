using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record AvDumpArgs(int LocalFileId) : CommandArgs($"AVDump_{LocalFileId}", CommandPriority.Normal, QueueType.Hash);
