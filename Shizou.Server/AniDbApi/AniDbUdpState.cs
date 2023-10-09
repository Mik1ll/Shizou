using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Nat;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi;

public sealed class AniDbUdpState : IDisposable
{
    private readonly Func<IAuthRequest> _authRequestFactory;
    private readonly Timer _bannedTimer;
    private readonly ILogger<AniDbUdpState> _logger;
    private readonly Func<ILogoutRequest> _logoutRequestFactory;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly Timer _logoutTimer;
    private readonly Timer? _mappingTimer;
    private readonly string _serverHost;
    private readonly ushort _serverPort;
    private bool _banned;
    private bool _loggedIn;
    private INatDevice? _router;

    public AniDbUdpState(
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        ILogger<AniDbUdpState> logger,
        Func<IAuthRequest> authRequestFactory,
        Func<ILogoutRequest> logoutRequestFactory,
        IDbContextFactory<ShizouContext> contextFactory)
    {
        _authRequestFactory = authRequestFactory;
        _logoutRequestFactory = logoutRequestFactory;
        _contextFactory = contextFactory;
        var options = optionsMonitor.CurrentValue;
        _serverHost = options.AniDb.ServerHost;
        _serverPort = options.AniDb.UdpServerPort;
        UdpClient = new UdpClient(options.AniDb.ClientPort, AddressFamily.InterNetwork);
        _logger = logger;

        _bannedTimer = new Timer(BanPeriod.TotalMilliseconds);
        _bannedTimer.Elapsed += (_, _) =>
        {
            _logger.LogInformation("Udp ban timer has elapsed: {BanPeriod}", BanPeriod);
            Banned = false;
        };
        _bannedTimer.AutoReset = false;
        using var context = _contextFactory.CreateDbContext();
        var currentBan = context.Timers.FirstOrDefault(t => t.Type == TimerType.UdpBan)?.Expires;
        if (currentBan is not null)
            if (currentBan > DateTime.UtcNow)
            {
                _bannedTimer.Interval = (currentBan.Value - DateTime.UtcNow).TotalMilliseconds;
                Banned = true;
            }


        _logoutTimer = new Timer(LogoutPeriod.TotalMilliseconds);
        _logoutTimer.Elapsed += (_, _) => { Logout().Wait(); };
        _logoutTimer.AutoReset = false;

        NatUtility.DeviceFound += (_, e) => _router = _router?.NatProtocol == NatProtocol.Pmp ? _router : e.Device;
        NatUtility.StartDiscovery();
        Task.Delay(2000).Wait();
        NatUtility.StopDiscovery();

        if (_router is null)
        {
            logger.LogInformation("Could not find router, assuming no IP masquerading");
        }
        else
        {
            _logger.LogInformation("Creating port mapping on port {AniDbClientPort}", options.AniDb.ClientPort);
            var mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, options.AniDb.ClientPort,
                options.AniDb.ClientPort));
            if (mapping.Lifetime > 0)
            {
                _mappingTimer = new Timer(TimeSpan.FromSeconds(mapping.Lifetime - 60).TotalMilliseconds);
                _mappingTimer.Elapsed += (_, _) =>
                {
                    var optionsVal = optionsMonitor.CurrentValue;
                    _logger.LogInformation("Recreating port mapping on port {AniDbClientPort}", optionsVal.AniDb.ClientPort);
                    mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, optionsVal.AniDb.ClientPort, optionsVal.AniDb.ClientPort));
                };
                _mappingTimer.AutoReset = true;
                _mappingTimer.Start();
            }
        }
    }


    public TimeSpan BanPeriod { get; } = new(12, 0, 0);
    public TimeSpan LogoutPeriod { get; } = new(0, 10, 0);

    public UdpClient UdpClient { get; }
    public string? SessionKey { get; set; }

    public bool LoggedIn
    {
        get => _loggedIn;
        set
        {
            _loggedIn = value;
            if (value)
            {
                _logoutTimer.Stop();
                _logoutTimer.Start();
            }
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
                var bannedTimer = context.Timers.FirstOrDefault(t => t.Type == TimerType.UdpBan);
                var banExpires = DateTime.UtcNow + BanPeriod;
                if (bannedTimer is null)
                    context.Timers.Add(new Data.Models.Timer
                    {
                        Type = TimerType.UdpBan,
                        Expires = banExpires
                    });
                else
                    bannedTimer.Expires = banExpires;
                context.SaveChanges();
            }
            else
            {
                BanReason = null;
            }
        }
    }

    public string? BanReason { get; set; }

    public void Dispose()
    {
        Logout().Wait();
        _bannedTimer.Dispose();
        _logoutTimer.Dispose();
        UdpClient.Dispose();
        _mappingTimer?.Dispose();
    }

    public void Connect()
    {
        UdpClient.Connect(_serverHost, _serverPort);
    }

    public async Task<bool> Login()
    {
        if (LoggedIn)
        {
            _logoutTimer.Stop();
            _logoutTimer.Start();
            return true;
        }

        var req = _authRequestFactory();
        _logger.LogInformation("Attempting to log into AniDB");
        req.SetParameters();
        await req.Process();
        if (LoggedIn)
        {
            _logger.LogInformation("Logged into AniDB");
            return true;
        }

        _logger.LogWarning("Failed to log into AniDB");
        return false;
    }

    public async Task<bool> Logout()
    {
        if (!LoggedIn)
            return true;
        var req = _logoutRequestFactory();
        req.SetParameters();
        await req.Process();
        if (!LoggedIn)
            return true;
        return false;
    }
}
