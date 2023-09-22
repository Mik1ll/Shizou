using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi;

namespace Shizou.Server.CommandProcessors;

public class AniDbUdpProcessor : CommandProcessor
{
    private readonly AniDbUdpState _aniDbUdpState;

    public AniDbUdpProcessor(ILogger<AniDbUdpProcessor> logger, IServiceProvider provider, AniDbUdpState aniDbUdpState,
        IDbContextFactory<ShizouContext> contextFactory, IServiceScopeFactory scopeFactory)
        : base(logger, QueueType.AniDbUdp, contextFactory, scopeFactory)
    {
        _aniDbUdpState = aniDbUdpState;
    }

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

    public override void Shutdown()
    {
        _aniDbUdpState.Logout().Wait();
    }
}
