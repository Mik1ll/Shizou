using System;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi;

public class AniDbHttpState : IDisposable
{
    private readonly Timer _bannedTimer;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILogger<AniDbHttpState> _logger;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private bool _banned;

    public AniDbHttpState(ILogger<AniDbHttpState> logger, IOptionsMonitor<ShizouOptions> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;

        _bannedTimer = new Timer(BanPeriod);
        _bannedTimer.Elapsed += (_, _) =>
        {
            _logger.LogInformation("Http ban timer has elapsed: {BanPeriod}", BanPeriod);
            if (Math.Abs(_bannedTimer.Interval - BanPeriod.TotalMilliseconds) > 1)
                _bannedTimer.Interval = BanPeriod.TotalMilliseconds;
            Banned = false;
        };
        _bannedTimer.AutoReset = false;

        var options = optionsMonitor.CurrentValue;
        var currentBan = options.AniDb.HttpBannedUntil;
        if (currentBan is not null)
            if (currentBan.Value > DateTimeOffset.UtcNow)
            {
                _bannedTimer.Interval = (currentBan.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
                Banned = true;
            }
            else
            {
                options.AniDb.HttpBannedUntil = null;
                options.SaveToFile();
            }
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
                var options = _optionsMonitor.CurrentValue;
                options.AniDb.HttpBannedUntil = DateTimeOffset.UtcNow + BanPeriod;
                options.SaveToFile();
            }
        }
    }

    public TimeSpan BanPeriod { get; } = new(12, 0, 0);

    public void Dispose()
    {
        _bannedTimer.Dispose();
    }
}
