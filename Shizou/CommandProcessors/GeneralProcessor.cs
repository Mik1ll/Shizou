using System;
using Microsoft.Extensions.Logging;
using ShizouData.Enums;

namespace Shizou.CommandProcessors;

public class GeneralProcessor : CommandProcessor
{
    public GeneralProcessor(ILogger<GeneralProcessor> logger, IServiceProvider provider) : base(logger, provider, QueueType.General)
    {
    }
}
