﻿using System.Threading.Tasks;
using Shizou.Common.Enums;

namespace Shizou.Server.Commands;

public sealed record NoopArgs(int Testint) : CommandArgs($"{nameof(NoopCommand)}_{Testint}");

[Command(CommandType.Noop, CommandPriority.Normal, QueueType.General)]
public sealed class NoopCommand : BaseCommand<NoopArgs>
{
    public NoopCommand(NoopArgs commandArgs) : base(commandArgs)
    {
    }

    public override async Task Process()
    {
        await Task.Delay(10_000);
        Completed = true;
    }
}
