using System;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Nat;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Exceptions;
using Shizou.Server.Options;
using Timer = System.Timers.Timer;

namespace Shizou.Server.AniDbApi;

public sealed class AniDbUdpState : IDisposable
{
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly Func<IAuthRequest> _authRequestFactory;
    private readonly Timer _bannedTimer;
    private readonly ILogger<AniDbUdpState> _logger;
    private readonly Func<ILogoutRequest> _logoutRequestFactory;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;
    private readonly Timer _logoutTimer;
    private readonly Timer _mappingTimer;
    private readonly string _serverHost;
    private readonly ushort _serverPort;
    private readonly SemaphoreSlim _natLock = new(1, 1);
    private bool _banned;
    private INatDevice? _router;
    private Mapping? _natMapping;

    public AniDbUdpState(
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        ILogger<AniDbUdpState> logger,
        Func<IAuthRequest> authRequestFactory,
        Func<ILogoutRequest> logoutRequestFactory,
        IDbContextFactory<ShizouContext> contextFactory)
    {
        _optionsMonitor = optionsMonitor;
        _authRequestFactory = authRequestFactory;
        _logoutRequestFactory = logoutRequestFactory;
        _contextFactory = contextFactory;
        var options = optionsMonitor.CurrentValue;
        _serverHost = options.AniDb.ServerHost;
        _serverPort = options.AniDb.UdpServerPort;
        UdpClient = new UdpClient(options.AniDb.ClientPort, AddressFamily.InterNetwork);
        UdpClient.Client.ReceiveBufferSize = 1 << 15;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            UdpClient.AllowNatTraversal(true);
        _logger = logger;

        _bannedTimer = new Timer();
        _bannedTimer.AutoReset = false;
        _bannedTimer.Elapsed += (_, _) =>
        {
            _logger.LogInformation("Udp ban timer has elapsed: {BanPeriod}", BanPeriod);
            Banned = false;
        };
        using var context = _contextFactory.CreateDbContext();
        var currentBan = context.Timers.FirstOrDefault(t => t.Type == TimerType.UdpBan)?.Expires;
        if (currentBan is not null)
            if (currentBan > DateTime.UtcNow + TimeSpan.FromSeconds(5))
            {
                Banned = true;
                _bannedTimer.Interval = (currentBan.Value - DateTime.UtcNow).TotalMilliseconds;
                _bannedTimer.Start();
            }

        _logoutTimer = new Timer();
        _logoutTimer.AutoReset = false;
        _logoutTimer.Elapsed += async (_, _) => { await Logout(); };

        _mappingTimer = new Timer();

        SetupNat();
    }


    public TimeSpan BanPeriod { get; } = new(12, 0, 0);
    public TimeSpan LogoutPeriod { get; } = new(0, 10, 0);

    public UdpClient UdpClient { get; }
    public string? SessionKey { get; set; }

    public bool LoggedIn { get; set; }

    public bool Banned
    {
        get => _banned;
        set
        {
            _banned = value;
            if (!value) BanReason = null;
        }
    }

    public string? BanReason { get; set; }

    public void ResetBannedTimer()
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

    public void ResetLogoutTimer()
    {
        _logoutTimer.Stop();
        _logoutTimer.Interval = LogoutPeriod.TotalMilliseconds;
        _logoutTimer.Start();
    }

    public void Connect()
    {
        UdpClient.Connect(_serverHost, _serverPort);
    }

    /// <exception cref="AniDbUdpRequestException"></exception>
    public async Task<bool> Login()
    {
        if (LoggedIn)
        {
            ResetLogoutTimer();
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

    /// <exception cref="AniDbUdpRequestException"></exception>
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

    public void Dispose()
    {
        _bannedTimer.Dispose();
        _logoutTimer.Dispose();
        _mappingTimer.Dispose();
        UdpClient.Dispose();
        _natLock.Dispose();
    }

    private void SetupNat()
    {
        var stopSearchTokenSource = new CancellationTokenSource();

        void OnDeviceFound(object? _, DeviceEventArgs e)
        {
            // ReSharper disable once MethodSupportsCancellation
            _natLock.Wait();
            if (_router?.NatProtocol != NatProtocol.Pmp)
                _router = e.Device;
            if (_router.NatProtocol == NatProtocol.Pmp)
                stopSearchTokenSource.Cancel();
            _natLock.Release();
        }

        NatUtility.DeviceFound += OnDeviceFound;
        NatUtility.StartDiscovery();
        try
        {
            // ReSharper disable once MethodSupportsCancellation
            Task.Delay(1000).Wait(stopSearchTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }

        NatUtility.StopDiscovery();

        if (_router is null)
        {
            _logger.LogInformation("Could not find router, assuming no IP masquerading");
        }
        else
        {
            CreateNatMapping();
            if (_natMapping?.Lifetime > 0)
            {
                _mappingTimer.Interval = TimeSpan.FromSeconds(_natMapping.Lifetime - 60).TotalMilliseconds;
                _mappingTimer.Elapsed += (_, _) => { CreateNatMapping(); };
                _mappingTimer.AutoReset = true;
                _mappingTimer.Start();
            }
        }
    }

    private void CreateNatMapping()
    {
        var optionsVal = _optionsMonitor.CurrentValue;
        _logger.LogInformation("Creating port mapping on port {AniDbClientPort}", optionsVal.AniDb.ClientPort);
        _natMapping = _router.CreatePortMap(new Mapping(Protocol.Udp, optionsVal.AniDb.ClientPort, optionsVal.AniDb.ClientPort));
    }
}
