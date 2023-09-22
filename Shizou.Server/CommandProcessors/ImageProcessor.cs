﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public class ImageProcessor : CommandProcessor
{
    public ImageProcessor(ILogger<ImageProcessor> logger, IDbContextFactory<ShizouContext> contextFactory,
        IServiceScopeFactory scopeFactory, Func<CommandService> commandServiceFactory)
        : base(logger, QueueType.Image, contextFactory, scopeFactory, commandServiceFactory)
    {
    }
}
