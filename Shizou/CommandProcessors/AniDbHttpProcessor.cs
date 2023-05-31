using System;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using ShizouCommon.Enums;

namespace Shizou.CommandProcessors;

public class AniDbHttpProcessor : CommandProcessor
{
    private readonly AniDbHttpState _httpState;

    public AniDbHttpProcessor(ILogger<AniDbHttpProcessor> logger, IServiceProvider provider, AniDbHttpState httpState) : base(logger, provider,
        QueueType.AniDbHttp)
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
