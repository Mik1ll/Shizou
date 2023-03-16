using System;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;

namespace Shizou.CommandProcessors;

public class AniDbHttpProcessor : CommandProcessor
{
    private readonly AniDbHttpState _httpState;
    private bool _paused = true;

    private string? _pauseReason;

    public AniDbHttpProcessor(ILogger<CommandProcessor> logger, IServiceProvider provider, AniDbHttpState httpState) : base(logger, provider,
        QueueType.AniDbHttp)
    {
        _httpState = httpState;
    }
    
    public override bool Paused
    {
        get => _paused || _httpState.Banned;
        protected set => _paused = value;
    }

    public override string? PauseReason
    {
        get => _httpState.Banned ? "HTTP banned" : _pauseReason;
        protected set => _pauseReason = value;
    }
}