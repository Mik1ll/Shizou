using Microsoft.Extensions.Logging;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Enums;
using Shizou.Extensions;

namespace Shizou.CommandProcessors
{
    public abstract class CommandProcessor
    {
        private readonly object _pausedLock = new();
        protected readonly ShizouContext Context;
        protected readonly ILogger<CommandProcessor> Logger;

        public readonly QueueType QueueType;
        private bool _paused;

        protected CommandProcessor(ILogger<CommandProcessor> logger, ShizouContext context, QueueType queueType)
        {
            Logger = logger;
            Context = context;
            QueueType = queueType;
        }

        public bool Paused
        {
            get
            {
                lock (_pausedLock)
                {
                    return _paused;
                }
            }
            set
            {
                lock (_pausedLock)
                {
                    _paused = value;
                }
            }
        }

        public int QueueCount => Context.CommandRequests.GetQueueCount(QueueType);

        public void ClearQueue()
        {
            Context.CommandRequests.ClearQueue(QueueType);
        }
    }
}
