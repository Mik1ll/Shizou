using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public class GeneralProcessor : CommandProcessor
{
    public GeneralProcessor(ILogger<GeneralProcessor> logger, IDbContextFactory<ShizouContext> contextFactory,
        IServiceScopeFactory scopeFactory, Func<CommandService> commandServiceFactory) : base(logger, QueueType.General, contextFactory, scopeFactory,
        commandServiceFactory)
    {
    }
}
