using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class HashProcessor : CommandProcessor
{
    public HashProcessor(ILogger<HashProcessor> logger, IServiceProvider provider, IDbContextFactory<ShizouContext> contextFactory)
        : base(logger, provider, QueueType.Hash, contextFactory)
    {
    }
}