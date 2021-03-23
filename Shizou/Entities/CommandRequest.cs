using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Shizou.Commands;

namespace Shizou.Entities
{
    public class CommandRequest : Entity
    {
        private static readonly Dictionary<CommandType, Func<BaseCommand>> Commands = new()
        {
            {CommandType.Noop, () => new NoopCommand()}
        };

        public CommandType Type { get; init; }

        public CommandPriority Priority { get; init; }
        
        public QueueType QueueType { get; init; }

        public string CommandId { get; init; } = string.Empty;

        public string CommandParams { get; init; } = string.Empty;

        [Computed] public BaseCommand Command => Commands[Type]().Init();
    }
}
