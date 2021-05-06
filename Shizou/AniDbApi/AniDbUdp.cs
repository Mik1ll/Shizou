using System;
using System.Net.Sockets;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.AniDbApi
{
    public class AniDbUdp : IDisposable
    {
        private readonly IOptionsMonitor<ShizouOptions> _options;
        private readonly ILogger<AniDbUdp> _logger;
        
        public bool LoggedIn { get; set; }
        public bool Banned { get; set; }
        public TimeSpan BanPeriod { get; } = new(12, 0, 0);
        public Timer BannedTimer { get; }
        public TimeSpan LogoutPeriod { get; } = new(0, 30, 0);
        public Timer LogoutTimer { get; }
        
        public bool Online { get; private set; }
        

        private Socket _udpSocket;


        public AniDbUdp(IOptionsMonitor<ShizouOptions> options, ILogger<AniDbUdp> logger)
        {
            _options = options;
            _logger = logger;
            BannedTimer = new(BanPeriod.TotalMilliseconds);
            BannedTimer.Elapsed += BanTimerElapsed;
            BannedTimer.AutoReset = false;
            LogoutTimer = new(LogoutPeriod.TotalMilliseconds);
            LogoutTimer.Elapsed += LogoutElapsed;
            LogoutTimer.AutoReset = true;
        }

        private void BanTimerElapsed(object s, ElapsedEventArgs e)
        {
            _logger.LogInformation($"Udp ban timer has elapsed: {BanPeriod}");
            Banned = false;
        }

        private void LogoutElapsed(object s, ElapsedEventArgs e)
        {
            if (LoggedIn)
                Logout();
        }

        private void CloseConnection()
        {
            
        }

        public bool Login()
        {
            throw new NotImplementedException();
        }

        public bool Logout()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _logger.LogInformation("Closing AniDb connection");
            _udpSocket.Dispose();
            BannedTimer.Dispose();
            LogoutTimer.Dispose();
        }
    }
}
