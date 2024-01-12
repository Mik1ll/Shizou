using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record ExtractThumbnailArgs(int LocalFileId) : CommandArgs($"MediaInfo_{LocalFileId}", CommandPriority.Normal, QueueType.General);
