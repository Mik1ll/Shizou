using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.Entities;

namespace Shizou.CommandProcessors
{
    public abstract class CommandProcessor : BackgroundService
    {
        public readonly QueueType QueueType = QueueType.Invalid;

        protected CommandProcessor(ILogger<CommandProcessor> logger)
        {
            Logger = logger;
        }

        protected ILogger<CommandProcessor> Logger { get; }

        public bool ProcessingCommand { get; protected set; }

        public CommandRequest? CurrentCommand { get; protected set; }

        public bool Paused { get; set; }
        public string? PauseReason { get; set; }
    }
}
