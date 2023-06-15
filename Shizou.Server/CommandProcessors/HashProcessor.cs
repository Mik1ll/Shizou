using System;
using Microsoft.Extensions.Logging;
using Shizou.Common.Enums;

namespace Shizou.Server.CommandProcessors;

public class HashProcessor : CommandProcessor
{
    public HashProcessor(ILogger<HashProcessor> logger, IServiceProvider provider)
        : base(logger, provider, QueueType.Hash)
    {
    }
}