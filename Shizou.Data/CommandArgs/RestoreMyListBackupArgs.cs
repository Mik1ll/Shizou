﻿using Shizou.Data.Enums;

namespace Shizou.Data.CommandArgs;

public record RestoreMyListBackupArgs
    (DateOnly? Date = null, string? Path = null) : CommandArgs("RestoreMyListBackup", CommandPriority.Low, QueueType.AniDbHttp);
