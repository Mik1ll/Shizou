﻿using Shizou.Data.Enums;

namespace Shizou.Data.CommandArgs;

public record GetImageCommandArgs
    (string Url, string SavePath) : CommandArgs($"GetImage_url={Url}_savePath={SavePath}", CommandPriority.Normal, QueueType.Image);
