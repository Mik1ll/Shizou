using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Enums;

namespace Shizou.CommandProcessors
{
    public abstract class CommandProcessor
    {
        protected readonly ILogger<CommandProcessor> Logger;

        public static readonly QueueType QueueType = QueueType.Invalid;
        
        public bool Paused { get; set; }

        protected CommandProcessor(ILogger<CommandProcessor> logger)
        {
            Logger = logger;
        }
    }
}
