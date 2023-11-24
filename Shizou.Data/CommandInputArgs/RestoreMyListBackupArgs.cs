using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record RestoreMyListBackupArgs
    (DateOnly? Date = null, string? Path = null) : CommandArgs("RestoreMyListBackup", CommandPriority.Low, QueueType.AniDbHttp);
