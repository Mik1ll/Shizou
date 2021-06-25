using System;
using Microsoft.Extensions.Logging;

namespace Shizou.CommandProcessors
{
    public class HashProcessor : CommandProcessor
    {
        public HashProcessor(ILogger<CommandProcessor> logger, IServiceProvider provider)
            : base(logger, provider, QueueType.Hash)
        {
        }
    }
}
