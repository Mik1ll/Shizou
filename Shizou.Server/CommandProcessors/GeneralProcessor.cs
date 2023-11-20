using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public class GeneralProcessor : CommandProcessor
{
    public GeneralProcessor(ILogger<GeneralProcessor> logger, IShizouContextFactory contextFactory,
        IServiceScopeFactory scopeFactory, Func<CommandService> commandServiceFactory) : base(logger, QueueType.General, contextFactory, scopeFactory,
        commandServiceFactory)
    {
    }
}
