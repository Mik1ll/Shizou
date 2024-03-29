﻿using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public sealed record ProcessArgs(int Id, IdTypeLocalOrFile IdType) : CommandArgs($"Process_id={Id}_type={IdType}", CommandPriority.Normal, QueueType.AniDbUdp);
