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

    public override bool Paused
    {
        get => _aniDbUdpState.Banned || base.Paused;
        protected set
        {
            if (!value && _aniDbUdpState.Banned)
                Logger.LogWarning("Can't unpause, UDP banned");
            else
                base.Paused = value;
        }
    }

    public override string? PauseReason
    {
        get => _aniDbUdpState is { Banned: true, BanReason: not null } ? _aniDbUdpState.BanReason : base.PauseReason;
        protected set => base.PauseReason = value;
    }

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
