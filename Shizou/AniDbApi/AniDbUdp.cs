using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.AniDbApi
{
    public class AniDbUdp
    {
        private readonly IOptionsMonitor<ShizouOptions> _options;        
        
        public bool LoggedIn { get; set; }
        public bool Banned { get; set; }


        public AniDbUdp(IOptionsMonitor<ShizouOptions> options)
        {
            _options = options;
        }
    }
}
