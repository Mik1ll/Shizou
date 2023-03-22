using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Nat;
using Shizou.AniDbApi.RateLimiters;
using Shizou.AniDbApi.Requests.Udp;
using Shizou.Options;

namespace Shizou.AniDbApi;

public sealed class AniDbUdpState : IDisposable
{
    private readonly Timer _bannedTimer;
    private readonly ILogger<AniDbUdpState> _logger;
    private readonly Timer _logoutTimer;
    private readonly Timer? _mappingTimer;
    private readonly IServiceProvider _provider;
    private bool _banned;
    private bool _loggedIn;
    private INatDevice? _router;

    public AniDbUdpState(IOptionsMonitor<ShizouOptions> options,
        ILogger<AniDbUdpState> logger, UdpRateLimiter rateLimiter, IServiceProvider provider)
    {
        _provider = provider;
        RateLimiter = rateLimiter;
        UdpClient = new UdpClient(options.CurrentValue.AniDb.ClientPort, AddressFamily.InterNetwork);
        UdpClient.Connect(options.CurrentValue.AniDb.ServerHost, options.CurrentValue.AniDb.UdpServerPort);
        _logger = logger;

        _bannedTimer = new Timer(BanPeriod.TotalMilliseconds);
        _bannedTimer.Elapsed += (_, _) =>
        {
            _logger.LogInformation("Udp ban timer has elapsed: {BanPeriod}", BanPeriod);
            Banned = false;
        };
        _bannedTimer.AutoReset = false;

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
            _logger.LogInformation($"Creating port mapping on port {options.CurrentValue.AniDb.ClientPort}");
            var mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, options.CurrentValue.AniDb.ClientPort, options.CurrentValue.AniDb.ClientPort));
            if (mapping.Lifetime > 0)
            {
                _mappingTimer = new Timer(TimeSpan.FromSeconds(mapping.Lifetime - 60).TotalMilliseconds);
                _mappingTimer.Elapsed += (_, _) =>
                {
                    _logger.LogInformation($"Recreating port mapping on port {options.CurrentValue.AniDb.ClientPort}");
                    mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, options.CurrentValue.AniDb.ClientPort,
                        options.CurrentValue.AniDb.ClientPort));
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

    public string? ImageServerUrl { get; set; }

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
                BanEndTime = DateTimeOffset.UtcNow + BanPeriod;
            }
            else
            {
                BanReason = null;
                BanEndTime = null;
            }
        }
    }

    public string? BanReason { get; set; }
    public DateTimeOffset? BanEndTime { get; private set; }

    public void Dispose()
    {
        Logout().Wait();
        _bannedTimer.Dispose();
        _logoutTimer.Dispose();
        UdpClient.Dispose();
        _mappingTimer?.Dispose();
    }

    public async Task<bool> Login()
    {
        if (LoggedIn)
        {
            _logoutTimer.Stop();
            _logoutTimer.Start();
            return true;
        }
        var req = new AuthRequest(_provider);
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
        var req = new LogoutRequest(_provider);
        await req.Process();
        if (!LoggedIn)
            return true;
        return false;
    }
}
