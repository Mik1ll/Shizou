using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly IShizouContextFactory _contextFactory;
    private readonly Timer _logoutTimer;
    private readonly Timer _mappingTimer;
    private readonly SemaphoreSlim _natLock = new(1, 1);
    private bool _banned;
    private INatDevice? _router;
    private Mapping? _natMapping;

    public AniDbUdpState(
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        ILogger<AniDbUdpState> logger,
        Func<IAuthRequest> authRequestFactory,
        Func<ILogoutRequest> logoutRequestFactory,
        IShizouContextFactory contextFactory)
    {
        _optionsMonitor = optionsMonitor;
        _authRequestFactory = authRequestFactory;
        _logoutRequestFactory = logoutRequestFactory;
        _contextFactory = contextFactory;
        var options = optionsMonitor.CurrentValue;
        ServerHost = options.AniDb.ServerHost;
        ServerPort = options.AniDb.UdpServerPort;
        UdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, options.AniDb.UdpClientPort));
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
#pragma warning disable VSTHRD101
        _logoutTimer.Elapsed += async (_, _) => await LogoutAsync().ConfigureAwait(false);
#pragma warning restore VSTHRD101

        _mappingTimer = new Timer();
        _logoutTimer.AutoReset = false;
#pragma warning disable VSTHRD101
        _mappingTimer.Elapsed += async (_, _) => await CreateNatMappingAsync().ConfigureAwait(false);
#pragma warning restore VSTHRD101
    }

    public string ServerHost { get; }
    public int ServerPort { get; }

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

    /// <exception cref="AniDbUdpRequestException"></exception>
    public async Task LoginAsync()
    {
        if (LoggedIn)
        {
            ResetLogoutTimer();
            return;
        }

        var req = _authRequestFactory();
        _logger.LogInformation("Attempting to log into AniDB");
        req.SetParameters();
        await req.ProcessAsync().ConfigureAwait(false);
    }

    /// <exception cref="AniDbUdpRequestException"></exception>
    public async Task LogoutAsync()
    {
        if (!LoggedIn)
            return;
        var req = _logoutRequestFactory();
        req.SetParameters();
        await req.ProcessAsync().ConfigureAwait(false);
    }

    public async Task SetupNatAsync()
    {
        var stopSearchTokenSource = new CancellationTokenSource();

#pragma warning disable VSTHRD100
        async void OnDeviceFound(object? sender, DeviceEventArgs e)
#pragma warning restore VSTHRD100
        {
            // ReSharper disable once MethodSupportsCancellation
            await _natLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_router?.NatProtocol != NatProtocol.Pmp)
                    _router = e.Device;
                if (_router.NatProtocol == NatProtocol.Pmp)
                    await stopSearchTokenSource.CancelAsync().ConfigureAwait(false);
            }
            finally
            {
                _natLock.Release();
            }
        }

        NatUtility.DeviceFound -= OnDeviceFound;
        NatUtility.DeviceFound += OnDeviceFound;
        NatUtility.StartDiscovery();
        try
        {
            await Task.Delay(5000, stopSearchTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        NatUtility.StopDiscovery();

        if (_router is null)
            _logger.LogInformation("Could not find router, assuming no IP masquerading");
        else
            await CreateNatMappingAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _bannedTimer.Dispose();
        _logoutTimer.Dispose();
        _mappingTimer.Dispose();
        UdpClient.Dispose();
        _natLock.Dispose();
    }

    private async Task CreateNatMappingAsync()
    {
        if (_router is null)
            return;
        var optionsVal = _optionsMonitor.CurrentValue;
        _natMapping = await _router.CreatePortMapAsync(_natMapping ?? new Mapping(Protocol.Udp, optionsVal.AniDb.UdpClientPort, optionsVal.AniDb.UdpClientPort))
            .ConfigureAwait(false);
        _logger.LogInformation("Created port mapping on port private: {ClientPrivatePort} public: {ClientPublicPort} with NAT type: {NatType}",
            _natMapping.PrivatePort, _natMapping.PublicPort, _router.NatProtocol);
        if (_natMapping?.Lifetime > 0)
        {
            _mappingTimer.Interval = TimeSpan.FromSeconds(_natMapping.Lifetime * .9).TotalMilliseconds;
            _mappingTimer.Start();
        }
    }
}
