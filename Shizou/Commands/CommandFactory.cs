using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Shizou.Entities;
using ILogger = Serilog.ILogger;

namespace Shizou.Commands
{
    public class CommandFactory
    {
        private Dictionary<CommandType, Func<CommandRequest, BaseCommand>> commands;
        private ILogger<CommandFactory> _logger;

        public CommandFactory(ILogger<CommandFactory> logger)
        {
            _logger = logger;
            commands = new Dictionary<CommandType, Func<CommandRequest, BaseCommand>>()
            {
                {CommandType.Noop, cr => new NoopCommand(cr)}
            };
        }

        public BaseCommand GetCommand(CommandRequest commandRequest)
        {
            return commands[commandRequest.Type](commandRequest).FromCommandRequest();
        }
    }
}
