using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi;
using Shizou.Server.Exceptions;
using Shizou.Server.Services;

namespace Shizou.Server.CommandProcessors;

public class AniDbUdpProcessor : CommandProcessor
{
    private readonly AniDbUdpState _aniDbUdpState;

    public AniDbUdpProcessor(ILogger<AniDbUdpProcessor> logger, AniDbUdpState aniDbUdpState,
        IDbContextFactory<ShizouContext> contextFactory, IServiceScopeFactory scopeFactory, Func<CommandService> commandServiceFactory)
        : base(logger, QueueType.AniDbUdp, contextFactory, scopeFactory, commandServiceFactory)
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

    protected override void ShutdownInner()
    {
        try
        {
            _aniDbUdpState.LogoutAsync().Wait();
        }
        catch (AniDbUdpRequestException)
        {
        }
    }
}
