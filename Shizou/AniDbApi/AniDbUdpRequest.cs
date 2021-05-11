using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;

namespace Shizou.AniDbApi
{
    public abstract class AniDbUdpRequest
    {
        protected readonly ILogger<AniDbUdpRequest> Logger;
        private readonly UdpRateLimiter _rateLimiter;
        private readonly UdpClient _client;

        public abstract string CommandText { get; protected set; }
        public string? ResponseText { get; protected set; }
        public bool Errored { get; set; }
        public string? ErrorText { get; set; }
        public AniDbResponseCode? ResponseCode { get; protected set; }
        public Encoding Encoding { get; } = Encoding.UTF8;

        protected AniDbUdpRequest(UdpClient client, ILogger<AniDbUdpRequest> logger, UdpRateLimiter rateLimiter)
        {
            Logger = logger;
            _rateLimiter = rateLimiter;
            _client = client;
        }
        
        public void SendRequest()
        {
            _rateLimiter.Wait();
            // TODO: handle server 6xx errors
        }
    }
}
