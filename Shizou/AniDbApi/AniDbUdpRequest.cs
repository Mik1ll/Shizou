using System.Text;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;

namespace Shizou.AniDbApi
{
    public abstract class AniDbUdpRequest
    {
        protected readonly ILogger<AniDbUdpRequest> Logger;
        private readonly UdpRateLimiter _rateLimiter;

        public string CommandText { get; protected set; } = string.Empty;
        public string? ResponseText { get; protected set; }
        public bool Errored { get; set; }
        public string? ErrorText { get; set; }
        public AniDbResponseCode? ResponseCode { get; protected set; }
        public Encoding Encoding { get; } = Encoding.UTF8;

        protected AniDbUdpRequest(ILogger<AniDbUdpRequest> logger, UdpRateLimiter rateLimiter)
        {
            Logger = logger;
            _rateLimiter = rateLimiter;
        }
        
        public void SendRequest()
        {
            _rateLimiter.Wait();
        }
    }
}
