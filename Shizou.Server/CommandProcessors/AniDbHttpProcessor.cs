using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public class AniDbHttpProcessor : CommandProcessor
{
    private readonly AniDbHttpState _httpState;

    public AniDbHttpProcessor(ILogger<AniDbHttpProcessor> logger, AniDbHttpState httpState,
        IShizouContextFactory contextFactory, IServiceScopeFactory scopeFactory, Func<CommandService> commandServiceFactory) : base(logger,
        QueueType.AniDbHttp, contextFactory, scopeFactory, commandServiceFactory)
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
