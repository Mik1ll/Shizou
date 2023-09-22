using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class HashProcessor : CommandProcessor
{
    public HashProcessor(ILogger<HashProcessor> logger, IServiceProvider provider, IDbContextFactory<ShizouContext> contextFactory,
        IServiceScopeFactory scopeFactory)
        : base(logger, QueueType.Hash, contextFactory, scopeFactory)
    {
    }
}
