using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi;

namespace Shizou.Server.CommandProcessors;

public class AniDbHttpProcessor : CommandProcessor
{
    private readonly AniDbHttpState _httpState;

    public AniDbHttpProcessor(ILogger<AniDbHttpProcessor> logger, AniDbHttpState httpState,
        IShizouContextFactory contextFactory, IServiceScopeFactory scopeFactory) : base(logger,
        QueueType.AniDbHttp, contextFactory, scopeFactory)
    {
        _httpState = httpState;
    }

    public override string DisplayName { get; } = "AniDB HTTP";

    public override bool Paused => _httpState.Banned || base.Paused;

    public override string? PauseReason => _httpState.Banned ? "HTTP banned" : base.PauseReason;
}
