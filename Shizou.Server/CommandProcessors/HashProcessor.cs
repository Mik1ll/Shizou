using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class HashProcessor : CommandProcessor
{
    public HashProcessor(ILogger<HashProcessor> logger, IShizouContextFactory contextFactory, IServiceScopeFactory scopeFactory)
        : base(logger, QueueType.Hash, contextFactory, scopeFactory)
    {
    }

    public override string DisplayName { get; } = "Hash";
}
