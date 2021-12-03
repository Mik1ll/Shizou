using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests
{
    public sealed class MyListAddRequest : AniDbUdpRequest
    {
        private MyListAddRequest(IServiceProvider provider, bool edit, bool? watched, MyListState? state = null) : base(
            provider.GetRequiredService<ILogger<MyListAddRequest>>(),
            provider.GetRequiredService<AniDbUdp>(),
            provider.GetRequiredService<AniDbUdpProcessor>())
        {
            Params["edit"] = edit ? "1" : "0";
            if (state is not null)
                Params["state"] = ((int)state).ToString();
            if (watched is not null)
                Params["viewed"] = edit ? "1" : "0";
        }

        public MyListAddRequest(IServiceProvider provider, int fid, bool edit, bool? watched, MyListState? state = null) : this(provider, edit, watched, state)
        {
            Params["fid"] = fid.ToString();
        }

        public MyListAddRequest(IServiceProvider provider, int lid, bool? watched, MyListState? state = null) : this(provider, true, watched, state)
        {
            Params["lid"] = lid.ToString();
        }

        public MyListAddRequest(IServiceProvider provider, int aid, string epno, bool edit, bool? watched, MyListState? state = null) : this(provider, edit,
            watched, state)
        {
            Params["aid"] = aid.ToString();
            Params["epno"] = epno;
            Params["generic"] = "1";
        }

        public override string Command { get; } = "MYLISTADD";
        public override Dictionary<string, string> Params { get; } = new();

        public override Task Process()
        {
            throw new NotImplementedException();
        }
    }
}
