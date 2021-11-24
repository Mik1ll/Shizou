using System;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace Shizou.CommandProcessors
{
    public class AniDbHttpProcessor : CommandProcessor
    {
        private bool _banned;

        private readonly Timer _bannedTimer;
        private bool _paused = true;

        private string? _pauseReason;

        public AniDbHttpProcessor(ILogger<CommandProcessor> logger, IServiceProvider provider) : base(logger, provider,
            QueueType.AniDbHttp)
        {
            _bannedTimer = new Timer(BanPeriod.TotalMilliseconds);
            _bannedTimer.Elapsed += BanTimerElapsed;
            _bannedTimer.AutoReset = false;
        }

        public bool Banned
        {
            get => _banned;
            set
            {
                _banned = value;
                if (value)
                {
                    _bannedTimer.Stop();
                    _bannedTimer.Start();
                }
            }
        }

        public TimeSpan BanPeriod { get; } = TimeSpan.FromHours(12);

        public override bool Paused
        {
            get => _paused || Banned;
            protected set => _paused = value;
        }

        public override string? PauseReason
        {
            get => Banned ? "HTTP banned" : _pauseReason;
            protected set => _pauseReason = value;
        }

        private void BanTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Banned = false;
        }
    }
}
