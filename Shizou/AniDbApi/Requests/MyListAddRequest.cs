using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Results;
using Shizou.CommandProcessors;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests
{
    public sealed class MyListAddRequest : AniDbUdpRequest
    {
        public AniDbMyListAddResult? MyListResult { get; private set; }

        private MyListAddRequest(IServiceProvider provider, bool edit, bool? watched, MyListState? state = null, MyListFileState? fileState = null)
            : base(provider.GetRequiredService<ILogger<MyListAddRequest>>(),
                provider.GetRequiredService<AniDbUdp>(),
                provider.GetRequiredService<AniDbUdpProcessor>())
        {
            Params["edit"] = edit ? "1" : "0";
            if (state is not null)
                Params["state"] = ((int)state).ToString();
            DateTimeOffset? watchedDate = null;
            if (watched is not null)
            {
                Params["viewed"] = watched.Value ? "1" : "0";
                if (watched.Value)
                {
                    watchedDate = DateTimeOffset.UtcNow;
                    Params["viewdate"] = watchedDate.Value.ToUnixTimeSeconds().ToString();
                }
            }
            if (fileState is not null)
                Params["filestate"] = ((int)fileState).ToString();

            MyListResult = new AniDbMyListAddResult(null, state, watched, watchedDate?.UtcDateTime, fileState);
        }

        public MyListAddRequest(IServiceProvider provider, int fid, bool edit, bool? watched, MyListState? state = null)
            : this(provider, edit, watched, state, MyListFileState.Normal)
        {
            Params["fid"] = fid.ToString();
        }

        public MyListAddRequest(IServiceProvider provider, int lid, bool? watched, MyListState? state = null, MyListFileState? fileState = null)
            : this(provider, true, watched, state, fileState)
        {
            Params["lid"] = lid.ToString();
        }

        public MyListAddRequest(IServiceProvider provider, int aid, string epno, bool edit, bool? watched, MyListState? state = null,
            MyListFileState? fileState = null)
            : this(provider, edit, watched, state, fileState)
        {
            Params["aid"] = aid.ToString();
            Params["epno"] = epno;
            Params["generic"] = "1";
        }

        public override string Command { get; } = "MYLISTADD";
        public override Dictionary<string, string> Params { get; } = new();

        public override async Task Process()
        {
            await SendRequest();
            switch (ResponseCode)
            {
                case AniDbResponseCode.MyListAdded:
                    if (string.IsNullOrWhiteSpace(ResponseText))
                    {
                        Errored = true;
                        return;
                    }
                    if (Params["edit"] == "0" && Params.TryGetValue("fid", out _))
                        MyListResult = MyListResult! with { ListId = int.Parse(ResponseText) };
                    break;
                case AniDbResponseCode.MyListEdited:
                    break;
                case AniDbResponseCode.MulitipleFilesFound:
                    break;
                case AniDbResponseCode.FileInMyList:
                    if (string.IsNullOrWhiteSpace(ResponseText))
                    {
                        Errored = true;
                        return;
                    }
                    var dataArr = ResponseText.Split('|');
                    DateTime? watchedDate = dataArr[7] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[7])).UtcDateTime : null;
                    MyListResult = new AniDbMyListAddResult(int.Parse(dataArr[0]),
                        Enum.Parse<MyListState>(dataArr[6]),
                        watchedDate is null ? false : true,
                        watchedDate,
                        Enum.Parse<MyListFileState>(dataArr[11])
                    );
                    break;
                case AniDbResponseCode.NoSuchFile:
                    Errored = true;
                    break;
                case AniDbResponseCode.NoSuchAnime:
                    Errored = true;
                    break;
                case AniDbResponseCode.NoSuchMyListEntry:
                    Errored = true;
                    break;
            }
        }
    }
}
