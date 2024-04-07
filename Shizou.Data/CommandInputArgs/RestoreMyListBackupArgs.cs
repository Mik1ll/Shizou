using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record RestoreMyListBackupArgs(string Path) : CommandArgs($"RestoreMyListBackup_\"{Path}\"", CommandPriority.Low, QueueType.AniDbHttp);
