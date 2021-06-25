using System;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;

namespace Shizou.CommandProcessors
{
    public class AniDbUdpProcessor : CommandProcessor
    {
        private readonly AniDbUdp _udpApi;

        private bool _paused = true;

        private string? _pauseReason;

        public AniDbUdpProcessor(ILogger<AniDbUdpProcessor> logger, IServiceProvider provider, AniDbUdp udpApi)
            : base(logger, provider, QueueType.AniDbUdp)
        {
            _udpApi = udpApi;
        }

        public override bool Paused
        {
            get => _paused || _udpApi.Banned || _udpApi.Paused;
            set => _paused = value;
        }

        public override string? PauseReason
        {
            get => _udpApi.Banned && _udpApi.BanReason is not null ? _udpApi.BanReason :
                _udpApi.Paused && _udpApi.PauseReason is not null ? _udpApi.PauseReason :
                _pauseReason;
            set => _pauseReason = value;
        }

        public override void Shutdown()
        {
            _udpApi.Logout().Wait();
        }
    }
}
