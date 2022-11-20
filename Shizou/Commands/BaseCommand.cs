﻿using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Models;

namespace Shizou.Commands
{
    public interface ICommand
    {
        bool Completed { get; set; }
        CommandRequest CommandRequest { get; }
        string CommandId { get; }
        Task Process();
    }

    public abstract class BaseCommand<T> : ICommand where T : CommandParams
    {
        protected readonly ILogger<BaseCommand<T>> Logger;
        protected readonly IServiceProvider Provider;

        protected BaseCommand(IServiceProvider provider, ILogger<BaseCommand<T>> logger, T commandParams)
        {
            Provider = provider;
            CommandParams = commandParams;
            Logger = logger;
            CommandId = commandParams.CommandId;
        }

        protected T CommandParams { get; }
        public bool Completed { get; set; }
        public string CommandId { get; }

        public CommandRequest CommandRequest
        {
            get
            {
                var commandAttr = GetType().GetCustomAttribute<CommandAttribute>() ??
                                  throw new InvalidOperationException($"Could not load command attribute from {GetType().Name}");
                return new CommandRequest
                {
                    Type = commandAttr.Type,
                    Priority = commandAttr.Priority,
                    QueueType = commandAttr.QueueType,
                    CommandId = CommandId,
                    CommandParams = JsonSerializer.Serialize(CommandParams)
                };
            }
        }

        public abstract Task Process();
    }
}
