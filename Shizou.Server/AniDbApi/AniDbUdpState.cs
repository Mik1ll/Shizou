using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Nat;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi;

public sealed class AniDbUdpState : IDisposable
{
    private readonly Timer _bannedTimer;
    private readonly ILogger<AniDbUdpState> _logger;
    private readonly Timer _logoutTimer;
    private readonly Timer? _mappingTimer;
    private bool _banned;
    private bool _loggedIn;
    private INatDevice? _router;
    private readonly string _serverHost;
    private readonly ushort _serverPort;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly IServiceScopeFactory _scopeFactory;

    public AniDbUdpState(IOptionsMonitor<ShizouOptions> optionsMonitor,
        ILogger<AniDbUdpState> logger, UdpRateLimiter rateLimiter, IServiceScopeFactory scopeFactory)
    {
        _optionsMonitor = optionsMonitor;
        _scopeFactory = scopeFactory;
        var options = optionsMonitor.CurrentValue;
        _serverHost = options.AniDb.ServerHost;
        _serverPort = options.AniDb.UdpServerPort;
        RateLimiter = rateLimiter;
        UdpClient = new UdpClient(options.AniDb.ClientPort, AddressFamily.InterNetwork);
        _logger = logger;

        _bannedTimer = new Timer(BanPeriod.TotalMilliseconds);
        _bannedTimer.Elapsed += (_, _) =>
        {
            _logger.LogInformation("Udp ban timer has elapsed: {BanPeriod}", BanPeriod);
            if (Math.Abs(_bannedTimer.Interval - BanPeriod.TotalMilliseconds) > 1)
                _bannedTimer.Interval = BanPeriod.TotalMilliseconds;
            Banned = false;
        };
        _bannedTimer.AutoReset = false;
        var currentBan = options.AniDb.UdpBannedUntil;
        if (currentBan is not null)
            if (currentBan.Value > DateTimeOffset.UtcNow)
            {
                _bannedTimer.Interval = (currentBan.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
                Banned = true;
            }
            else
            {
                options.AniDb.UdpBannedUntil = null;
                options.SaveToFile();
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
            _logger.LogInformation("Creating port mapping on port {AniDbClientPort}", optionsMonitor.CurrentValue.AniDb.ClientPort);
            var mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, optionsMonitor.CurrentValue.AniDb.ClientPort,
                optionsMonitor.CurrentValue.AniDb.ClientPort));
            if (mapping.Lifetime > 0)
            {
                _mappingTimer = new Timer(TimeSpan.FromSeconds(mapping.Lifetime - 60).TotalMilliseconds);
                _mappingTimer.Elapsed += (_, _) =>
                {
                    _logger.LogInformation("Recreating port mapping on port {AniDbClientPort}", optionsMonitor.CurrentValue.AniDb.ClientPort);
                    mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, optionsMonitor.CurrentValue.AniDb.ClientPort,
                        optionsMonitor.CurrentValue.AniDb.ClientPort));
                };
                _mappingTimer.AutoReset = true;
                _mappingTimer.Start();
            }
        }
    }


    public TimeSpan BanPeriod { get; } = new(12, 0, 0);
    public TimeSpan LogoutPeriod { get; } = new(0, 30, 0);

    public UdpClient UdpClient { get; }
    public UdpRateLimiter RateLimiter { get; }
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
                _bannedTimer.Start();
                var options = _optionsMonitor.CurrentValue;
                options.AniDb.UdpBannedUntil = DateTimeOffset.UtcNow + BanPeriod;
                options.SaveToFile();
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
        using var scope = _scopeFactory.CreateScope();
        var udpRequestFactory = scope.ServiceProvider.GetRequiredService<UdpRequestFactory>();
        var req = udpRequestFactory.AuthRequest();
        _logger.LogInformation("Attempting to log into AniDB");
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
        using var scope = _scopeFactory.CreateScope();
        var udpRequestFactory = scope.ServiceProvider.GetRequiredService<UdpRequestFactory>();
        var req = udpRequestFactory.LogoutRequest();
        await req.Process();
        if (!LoggedIn)
            return true;
        return false;
    }
}
