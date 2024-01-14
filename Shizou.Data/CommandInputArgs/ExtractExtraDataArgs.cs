using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record ExtractExtraDataArgs(int LocalFileId) : CommandArgs($"ExtractExtraData_{LocalFileId}", CommandPriority.Normal, QueueType.General);
