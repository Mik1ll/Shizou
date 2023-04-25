using System;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using ShizouData.Enums;

namespace Shizou.CommandProcessors;

public class AniDbUdpProcessor : CommandProcessor
{
    private readonly AniDbUdpState _aniDbUdpState;

    private bool _paused = true;

    private string? _pauseReason;

    public AniDbUdpProcessor(ILogger<AniDbUdpProcessor> logger, IServiceProvider provider, AniDbUdpState aniDbUdpState)
        : base(logger, provider, QueueType.AniDbUdp)
    {
        _aniDbUdpState = aniDbUdpState;
    }

    public override bool Paused
    {
        get => _paused || _aniDbUdpState.Banned;
        protected set => _paused = value;
    }

    public override string? PauseReason
    {
        get => _aniDbUdpState.Banned && _aniDbUdpState.BanReason is not null ? _aniDbUdpState.BanReason : _pauseReason;
        protected set => _pauseReason = value;
    }

    public override void Shutdown()
    {
        _aniDbUdpState.Logout().Wait();
    }
}