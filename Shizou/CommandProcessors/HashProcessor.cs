using System;
using Microsoft.Extensions.Logging;
using ShizouCommon.Enums;

namespace Shizou.CommandProcessors;

public class HashProcessor : CommandProcessor
{
    public HashProcessor(ILogger<HashProcessor> logger, IServiceProvider provider)
        : base(logger, provider, QueueType.Hash)
    {
    }
}