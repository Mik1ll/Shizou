using Microsoft.Extensions.Logging;
using Serilog;
using Shizou.Repositories;

namespace Shizou.Commands
{
    public abstract class CommandProcessor
    {
        protected ILogger<CommandProcessor> Logger;
        protected ICommandRequestRepository CommandRequestRepository;

        protected CommandProcessor(ILogger<CommandProcessor> logger, ICommandRequestRepository commandRequestRepository, QueueType queueType)
        {
            Logger = logger;
            CommandRequestRepository = commandRequestRepository;
            QueueType = queueType;
        }
        
        private readonly object _pausedLock = new();
        private bool _paused;

        public readonly QueueType QueueType;

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

        public int QueueCount => CommandRequestRepository.GetQueueCount(QueueType);

        public void ClearQueue()
        {
            CommandRequestRepository.ClearQueue(QueueType);
        }
    }
}
