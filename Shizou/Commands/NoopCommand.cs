﻿using Shizou.CommandProcessors;
using Shizou.Enums;

namespace Shizou.Commands
{
    public sealed record NoopParams : CommandParams
    {
        public int Testint { get; set; }
    }

    [Command(CommandType.Noop, CommandPriority.Default, QueueType.General)]
    public sealed class NoopCommand : BaseCommand<NoopParams>
    {
        public NoopCommand(NoopParams commandParams) : base(commandParams)
        {
        }

        public override void Process()
        {
        }

        public override string CommandId => nameof(NoopCommand);
    }
}
