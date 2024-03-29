﻿using Microsoft.EntityFrameworkCore;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

[Index(nameof(CommandId), IsUnique = true)]
public class ScheduledCommand
{
    public int Id { get; set; }

    public required DateTime NextRunTime { get; set; }
    public int? RunsLeft { get; set; }
    public double? FrequencyMinutes { get; set; }
    public required CommandPriority Priority { get; set; }
    public required QueueType QueueType { get; set; }
    public required string CommandId { get; set; }
    public required CommandArgs CommandArgs { get; set; }
}
