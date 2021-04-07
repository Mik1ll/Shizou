using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Commands;

namespace Shizou.Entities
{
    public class CommandRequest : Entity
    {
        private static readonly Dictionary<CommandType, Func<BaseCommand>> Commands = new()
        {
            {CommandType.Noop, () => new NoopCommand()}
        };

        public CommandType Type { get; set; }

        public CommandPriority Priority { get; set; }

        public QueueType QueueType { get; set; }

        public string CommandId { get; set; } = string.Empty;

        public string CommandParams { get; set; } = string.Empty;

        [NotMapped] public BaseCommand Command => Commands[Type]().Init();
    }
}
