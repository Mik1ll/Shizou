using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class GeneralProcessor : CommandProcessor
{
    public GeneralProcessor(ILogger<GeneralProcessor> logger, IShizouContextFactory contextFactory, IServiceScopeFactory scopeFactory)
        : base(logger, QueueType.General, contextFactory, scopeFactory)
    {
    }

    public override string DisplayName { get; } = "General";
}
