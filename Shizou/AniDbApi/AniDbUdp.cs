using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Options;
using Mono.Nat;
using Shizou.CommandProcessors;
using Timer = System.Timers.Timer;

namespace Shizou.AniDbApi
{
    public sealed class AniDbUdp : IDisposable
    {
        private readonly IOptionsMonitor<ShizouOptions> _options;
        private readonly ILogger<AniDbUdp> _logger;
        private readonly UdpRateLimiter _rateLimiter;

        public bool LoggedIn { get; private set; }
        public bool Banned { get; private set; }
        private readonly TimeSpan _banPeriod = new(12, 0, 0);
        private readonly Timer _bannedTimer;
        private readonly TimeSpan _logoutPeriod = new(0, 30, 0);
        private readonly Timer _logoutTimer;


        private readonly UdpClient _udpClient;
        private INatDevice? _router;
        private Mapping? _mapping;
        private readonly Timer? _mappingTimer;

        public AniDbUdp(IOptionsMonitor<ShizouOptions> options, ILogger<AniDbUdp> logger, UdpRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
            _udpClient = new UdpClient(options.CurrentValue.AniDb.ClientPort, AddressFamily.InterNetwork);
            _udpClient.Connect(options.CurrentValue.AniDb.ServerHost, options.CurrentValue.AniDb.ServerPort);
            _options = options;
            _options.OnChange(OnOptionsChanged);
            _logger = logger;

            _bannedTimer = new Timer(_banPeriod.TotalMilliseconds);
            _bannedTimer.Elapsed += BanTimerElapsed;
            _bannedTimer.AutoReset = false;

            _logoutTimer = new Timer(_logoutPeriod.TotalMilliseconds);
            _logoutTimer.Elapsed += LogoutElapsed;
            _logoutTimer.AutoReset = false;

            NatUtility.DeviceFound += FoundRouter;
            NatUtility.StartDiscovery();
            Task.Delay(2000).Wait();
            NatUtility.StopDiscovery();

            _mapping = _router?.CreatePortMap(new Mapping(Protocol.Udp, _options.CurrentValue.AniDb.ClientPort, _options.CurrentValue.AniDb.ClientPort));
            if (_mapping?.Lifetime > 0)
            {
                _mappingTimer = new Timer(_mapping.Lifetime * 1000 - 10000);
                _mappingTimer.Elapsed += MappingElapsed;
                _mappingTimer.AutoReset = true;
                _mappingTimer.Start();
            }
        }

        private void OnOptionsChanged(ShizouOptions options)
        {
            ;
        }

        private void MappingElapsed(object s, ElapsedEventArgs e)
        {
            var oldLifetime = _mapping?.Lifetime;
            _mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, _options.CurrentValue.AniDb.ClientPort, _options.CurrentValue.AniDb.ClientPort));
            if (_mapping.Lifetime != oldLifetime)
                _mappingTimer!.Interval = _mapping.Lifetime * 1000 - 10000;
        }

        private void BanTimerElapsed(object s, ElapsedEventArgs e)
        {
            _logger.LogInformation($"Udp ban timer has elapsed: {_banPeriod}");
            Banned = false;
        }

        private void LogoutElapsed(object s, ElapsedEventArgs e)
        {
            if (LoggedIn)
                Logout();
            LoggedIn = false;
        }

        public bool Login()
        {
            if (LoggedIn)
                return true;

            bool result = false;
            
            if (!result)
                return false;
            LoggedIn = true;
            return true;
        }

        private void FoundRouter(object? s, DeviceEventArgs e) => _router = _router?.NatProtocol == NatProtocol.Pmp ? _router : e.Device;

        public bool Logout()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _logger.LogInformation("Closing AniDb connection");
            _bannedTimer.Dispose();
            _logoutTimer.Dispose();
            _udpClient.Dispose();
            _mappingTimer?.Dispose();
        }
    }
}
