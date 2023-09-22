using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class ImageProcessor : CommandProcessor
{
    public ImageProcessor(ILogger<ImageProcessor> logger, IServiceProvider provider, IDbContextFactory<ShizouContext> contextFactory,
        IServiceScopeFactory scopeFactory)
        : base(logger, QueueType.Image, contextFactory, scopeFactory)
    {
    }
}
