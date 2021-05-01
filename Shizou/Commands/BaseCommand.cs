using System;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shizou.Database;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Commands
{
    public abstract class BaseCommand
    {
        public bool Completed = false;

        // ReSharper disable once InconsistentNaming
        protected CommandParams _commandParams;
        protected abstract CommandParams CommandParams { get; }

        protected BaseCommand(CommandParams commandParams)
        {
            _commandParams = commandParams;
        }

        protected BaseCommand(CommandRequest commandRequest, Type commandParamType)
        {
            _commandParams = (CommandParams)JsonSerializer.Deserialize(commandRequest.CommandParams, commandParamType)!;
        }
        
        public CommandRequest CommandRequest
        {
            get
            {
                var commandAttr = GetType().GetCustomAttribute<CommandAttribute>();
                return new CommandRequest()
                {
                    Type = commandAttr?.Type ?? CommandType.Invalid,
                    Priority = commandAttr?.Priority ?? CommandPriority.Invalid,
                    QueueType = commandAttr?.QueueType ?? QueueType.Invalid,
                    CommandId = GenerateCommandId(),
                    CommandParams = JsonSerializer.Serialize(_commandParams, _commandParams.GetType())
                };
            }
        }

        public abstract void Process();

        protected abstract string GenerateCommandId();
    }
}
