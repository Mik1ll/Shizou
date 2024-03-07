using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class ImageProcessor : CommandProcessor
{
    public ImageProcessor(ILogger<ImageProcessor> logger, IShizouContextFactory contextFactory, IServiceScopeFactory scopeFactory)
        : base(logger, QueueType.Image, contextFactory, scopeFactory)
    {
    }

    public override string DisplayName { get; } = "Image";
}
