using Microsoft.Extensions.Logging;

namespace Shizou.CommandProcessors
{
    public abstract class CommandProcessor
    {
        public static readonly QueueType QueueType = QueueType.Invalid;
        protected readonly ILogger<CommandProcessor> Logger;

        protected CommandProcessor(ILogger<CommandProcessor> logger)
        {
            Logger = logger;
        }

        public bool Paused { get; set; }
    }
}
