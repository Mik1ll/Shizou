using System;
using Microsoft.Extensions.Logging;
using Shizou.Common.Enums;

namespace Shizou.Server.CommandProcessors;

public class GeneralProcessor : CommandProcessor
{
    public GeneralProcessor(ILogger<GeneralProcessor> logger, IServiceProvider provider) : base(logger, provider, QueueType.General)
    {
    }
}
