using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi;
using Shizou.Server.Exceptions;

namespace Shizou.Server.CommandProcessors;

public class AniDbUdpProcessor : CommandProcessor
{
    private readonly AniDbUdpState _aniDbUdpState;

    public AniDbUdpProcessor(ILogger<AniDbUdpProcessor> logger, AniDbUdpState aniDbUdpState,
        IShizouContextFactory contextFactory, IServiceScopeFactory scopeFactory)
        : base(logger, QueueType.AniDbUdp, contextFactory, scopeFactory)
    {
        _aniDbUdpState = aniDbUdpState;
    }

    public override string DisplayName { get; } = "AniDB UDP";

    public override bool Paused => _aniDbUdpState.Banned || base.Paused;

    public override string? PauseReason => _aniDbUdpState.Banned ? $"UDP banned with Reason: {_aniDbUdpState.BanReason ?? "<empty>"}" : base.PauseReason;

    protected override async Task OnShutdownAsync()
    {
        try
        {
            await _aniDbUdpState.LogoutAsync().ConfigureAwait(false);
        }
        catch (AniDbUdpRequestException)
        {
        }
    }
}
