using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Nat;
using Shizou.CommandProcessors;
using Shizou.Options;

namespace Shizou.AniDbApi
{
    public sealed class AniDbUdp : IDisposable
    {
        private readonly Timer _bannedTimer;
        private readonly TimeSpan _banPeriod = new(12, 0, 0);
        private readonly ILogger<AniDbUdp> _logger;
        private readonly TimeSpan _logoutPeriod = new(0, 30, 0);
        private readonly Timer _logoutTimer;
        private readonly Timer? _mappingTimer;
        private readonly IOptionsMonitor<ShizouOptions> _options;
        private readonly TimeSpan _pausePeriod = new(0, 30, 0);
        private readonly Timer _pauseTimer;
        private bool _banned;
        private bool _loggedIn;
        private Mapping? _mapping;
        private INatDevice? _router;

        public AniDbUdp(IOptionsMonitor<ShizouOptions> options,
            ILogger<AniDbUdp> logger, UdpRateLimiter rateLimiter
        )
        {
            RateLimiter = rateLimiter;
            UdpClient = new UdpClient(options.CurrentValue.AniDb.ClientPort, AddressFamily.InterNetwork);
            UdpClient.Connect(options.CurrentValue.AniDb.ServerHost, options.CurrentValue.AniDb.ServerPort);
            _options = options;
            _options.OnChange(OnOptionsChanged);
            _logger = logger;

            _bannedTimer = new Timer(_banPeriod.TotalMilliseconds);
            _bannedTimer.Elapsed += BanTimerElapsed;
            _bannedTimer.AutoReset = false;

            _logoutTimer = new Timer(_logoutPeriod.TotalMilliseconds);
            _logoutTimer.Elapsed += LogoutElapsed;
            _logoutTimer.AutoReset = false;

            _pauseTimer = new Timer(_pausePeriod.TotalMilliseconds);
            _pauseTimer.Elapsed += PausedElapsed;
            _pauseTimer.AutoReset = false;

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

        public UdpClient UdpClient { get; }
        public UdpRateLimiter RateLimiter { get; }
        public string? SessionKey { get; set; }

        public bool Paused { get; private set; }

        public bool LoggedIn
        {
            get => _loggedIn;
            private set
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
            private set
            {
                _banned = value;
                if (value)
                {
                    _bannedTimer.Stop();
                    _bannedTimer.Start();
                }
            }
        }

        public string? BanReason { get; private set; }

        public void Dispose()
        {
            _logger.LogInformation("Closing AniDb connection");
            _bannedTimer.Dispose();
            _logoutTimer.Dispose();
            UdpClient.Dispose();
            _mappingTimer?.Dispose();
        }

        private void PausedElapsed(object sender, ElapsedEventArgs e)
        {
            Paused = false;
        }

        private void OnOptionsChanged(ShizouOptions options)
        {
        }

        private void MappingElapsed(object sender, ElapsedEventArgs e)
        {
            var oldLifetime = _mapping?.Lifetime;
            _mapping = _router.CreatePortMap(new Mapping(Protocol.Udp, _options.CurrentValue.AniDb.ClientPort, _options.CurrentValue.AniDb.ClientPort));
            if (_mapping.Lifetime != oldLifetime)
                _mappingTimer!.Interval = _mapping.Lifetime * 1000 - 10000;
        }

        private void BanTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _logger.LogInformation($"Udp ban timer has elapsed: {_banPeriod}");
            Banned = false;
            BanReason = null;
        }

        private void LogoutElapsed(object sender, ElapsedEventArgs e)
        {
            // TODO: Logout command
            //if (LoggedIn)
            //Logout();
            LoggedIn = false;
        }

        private void FoundRouter(object? sender, DeviceEventArgs e)
        {
            _router = _router?.NatProtocol == NatProtocol.Pmp ? _router : e.Device;
        }


        // TODO: Move to AniDbUdpRequest
        public void HandleErrors(AniDbUdpRequest request)
        {
            if (request.Errored)
                switch (request.ResponseCode)
                {
                    // No response
                    case null:
                        break;
                    case AniDbResponseCode.ServerBusy:
                        break;
                    case AniDbResponseCode.Banned:
                        Banned = true;
                        BanReason = request.ResponseText;
                        _logger.LogWarning("Banned: {banReason}, waiting {hours}hr {minutes}min ({unbanTime})", BanReason, _banPeriod.Hours,
                            _banPeriod.Minutes, DateTime.Now + _banPeriod);
                        break;
                    case AniDbResponseCode.InvalidSession:
                        _logger.LogWarning("Invalid session, reauth");
                        LoggedIn = false;
                        break;
                    case AniDbResponseCode.LoginFirst:
                        _logger.LogWarning("Not logged in, reauth");
                        LoggedIn = false;
                        break;
                    case AniDbResponseCode.AccessDenied:
                        _logger.LogError("Access denied");
                        break;
                    case AniDbResponseCode.InternalServerError or (> AniDbResponseCode.ServerBusy and < (AniDbResponseCode)700):
                        _logger.LogCritical("AniDB Server CRITICAL ERROR {errorCode} : {errorCodeStr}", request.ResponseCode, request.ResponseCodeString);
                        break;
                    case AniDbResponseCode.UnknownCommand:
                        _logger.LogError("Unknown command");
                        // TODO: decide what to do here
                        break;
                    case AniDbResponseCode.IllegalInputOrAccessDenied:
                        _logger.LogError("Illegal input or access is denied");
                        // TODO: decide what to do here
                        break;
                    default:
                        if (!Enum.IsDefined(typeof(AniDbResponseCode), request.ResponseCode))
                            _logger.LogError("Response Code {ResponseCode} not found in enumeration: Code string: {codeString}", request.ResponseCode,
                                request.ResponseCodeString);
                        break;
                }
        }
    }
}
