using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class GeneralProcessor : CommandProcessor
{
    public GeneralProcessor(ILogger<GeneralProcessor> logger, IServiceProvider provider, IDbContextFactory<ShizouContext> contextFactory) : base(logger,
        provider, QueueType.General, contextFactory)
    {
    }
}
