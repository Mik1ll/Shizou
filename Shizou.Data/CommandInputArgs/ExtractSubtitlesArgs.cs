using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record ExtractSubtitlesArgs(int LocalFileId) : CommandArgs($"ExtractSubtitles_{LocalFileId}", CommandPriority.Normal, QueueType.General);
