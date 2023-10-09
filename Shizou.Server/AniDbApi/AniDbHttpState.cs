using System;
using System.Linq;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi;

public class AniDbHttpState : IDisposable
{
    private readonly Timer _bannedTimer;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILogger<AniDbHttpState> _logger;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private bool _banned;

    public AniDbHttpState(ILogger<AniDbHttpState> logger, IDbContextFactory<ShizouContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;

        _bannedTimer = new Timer(BanPeriod);
        _bannedTimer.Elapsed += (_, _) =>
        {
            _logger.LogInformation("Http ban timer has elapsed: {BanPeriod}", BanPeriod);
            Banned = false;
        };
        _bannedTimer.AutoReset = false;

        using var context = _contextFactory.CreateDbContext();
        var currentBan = context.Timers.FirstOrDefault(t => t.Type == TimerType.HttpBan)?.Expires;
        if (currentBan is not null)
            if (currentBan > DateTime.UtcNow)
            {
                _bannedTimer.Interval = (currentBan.Value - DateTime.UtcNow).TotalMilliseconds;
                Banned = true;
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
                _bannedTimer.Interval = BanPeriod.TotalMilliseconds;
                _bannedTimer.Start();
                using var context = _contextFactory.CreateDbContext();
                var bannedTimer = context.Timers.FirstOrDefault(t => t.Type == TimerType.HttpBan);
                var banExpires = DateTime.UtcNow + BanPeriod;
                if (bannedTimer is null)
                    context.Timers.Add(new Data.Models.Timer
                    {
                        Type = TimerType.HttpBan,
                        Expires = banExpires
                    });
                else
                    bannedTimer.Expires = banExpires;
                context.SaveChanges();
            }
        }
    }

    public TimeSpan BanPeriod { get; } = new(12, 0, 0);

    public void Dispose()
    {
        _bannedTimer.Dispose();
    }
}
