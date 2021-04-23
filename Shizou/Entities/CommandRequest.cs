using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Shizou.Commands;
using Shizou.Enums;

namespace Shizou.Entities
{
    [Index(nameof(CommandId), IsUnique = true)]
    public class CommandRequest
    {
        private static readonly Dictionary<CommandType, Func<BaseCommand>> Commands = Assembly.GetExecutingAssembly().GetTypes()
            .Select(t => new {type = t, commandAttr = t.GetCustomAttribute<CommandAttribute>()})
            .Where(x => x.commandAttr is not null && x.type.IsSubclassOf(typeof(BaseCommand)))
            .ToDictionary(x => x.commandAttr!.Type, x => new Func<BaseCommand>(() => (BaseCommand)Activator.CreateInstance(x.type)!));

        public int Id { get; set; }

        public CommandType Type { get; set; }
        public CommandPriority Priority { get; set; }
        public QueueType QueueType { get; set; }
        public string CommandId { get; set; } = string.Empty;
        public string CommandParams { get; set; } = string.Empty;
        
        [NotMapped] public BaseCommand Command => Commands[Type]().Init(this);
    }
}
