using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.AniDbApi.Requests.Udp;

public class GenericRequest : AniDbUdpRequest
{
    public GenericRequest(IServiceProvider provider, string command, Dictionary<string, string> args) : base(provider, command)
    {
        args.ToList().ForEach(a => Args.Add(a.Key, a.Value));
    }

    public override async Task Process()
    {
        await HandleRequest();
    }
}
