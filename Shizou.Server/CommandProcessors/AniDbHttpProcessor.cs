using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi;

namespace Shizou.Server.CommandProcessors;

public class AniDbHttpProcessor : CommandProcessor
{
    private readonly AniDbHttpState _httpState;

    public AniDbHttpProcessor(ILogger<AniDbHttpProcessor> logger, IServiceProvider provider, AniDbHttpState httpState,
        IDbContextFactory<ShizouContext> contextFactory, IServiceScopeFactory scopeFactory) : base(logger,
        QueueType.AniDbHttp, contextFactory, scopeFactory)
    {
        _httpState = httpState;
    }

    public override bool Paused
    {
        get => _httpState.Banned || base.Paused;
        protected set
        {
            if (!value && _httpState.Banned)
                Logger.LogWarning("Can't unpause, HTTP banned");
            else
                base.Paused = value;
        }
    }

    public override string? PauseReason
    {
        get => _httpState.Banned ? "HTTP banned" : base.PauseReason;
        protected set => base.PauseReason = value;
    }
}
