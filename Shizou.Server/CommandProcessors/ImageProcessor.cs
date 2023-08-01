using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.CommandProcessors;

public class ImageProcessor : CommandProcessor
{
    public ImageProcessor(ILogger<ImageProcessor> logger, IServiceProvider provider, IDbContextFactory<ShizouContext> contextFactory)
        : base(logger, provider, QueueType.Image, contextFactory)
    {
    }
}
