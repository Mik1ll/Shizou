using System;
using System.Threading.Tasks;
using Shizou.AniDbApi.Requests.Udp.Results;
using ShizouCommon.Enums;

namespace Shizou.AniDbApi.Requests.Udp;

public class MyListAddRequest : AniDbUdpRequest
{
    private readonly bool? _watched;
    private readonly DateTimeOffset? _watchedDate;
    private readonly MyListState? _state;
    private readonly MyListFileState? _fileState;
    public AniDbMyListAddResult? MyListResult { get; private set; }

    private MyListAddRequest(IServiceProvider provider, bool edit, bool? watched, DateTimeOffset? watchedDate, MyListState? state, MyListFileState? fileState)
        : base(provider, "MYLISTADD")
    {
        _watched = watched;
        _watchedDate = watchedDate;
        _state = state;
        _fileState = fileState;
        Args["edit"] = edit ? "1" : "0";
        if (watched is not null)
            Args["viewed"] = watched.Value ? "1" : "0";
        if (watchedDate is not null)
            Args["viewdate"] = watchedDate.Value.ToUnixTimeSeconds().ToString();
        if (state is not null)
            Args["state"] = ((int)state).ToString();
        if (fileState is not null)
            Args["filestate"] = ((int)fileState).ToString();
    }

    public MyListAddRequest(IServiceProvider provider, int fid, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null,
        MyListFileState? fileState = null) : this(provider, edit, watched, watchedDate, state, fileState)
    {
        Args["fid"] = fid.ToString();
    }

    public MyListAddRequest(IServiceProvider provider, int lid, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null,
        MyListFileState? fileState = null) : this(provider, true, watched, watchedDate, state, fileState)
    {
        Args["lid"] = lid.ToString();
    }

    public MyListAddRequest(IServiceProvider provider, int aid, string epno, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null,
        MyListState? state = null, MyListFileState? fileState = null) : this(provider, edit, watched, watchedDate, state, fileState)
    {
        Args["aid"] = aid.ToString();
        Args["epno"] = epno;
        Args["generic"] = "1";
    }
    
    public override async Task Process()
    {
        await HandleRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (string.IsNullOrWhiteSpace(ResponseText))
                {
                    return;
                }
                if (Args["edit"] == "0" && Args.ContainsKey("fid"))
                    MyListResult = new AniDbMyListAddResult(int.Parse(ResponseText), DateTimeOffset.UtcNow, _state, _watched, _watchedDate, _fileState);
                break;
            case AniDbResponseCode.MyListEdited:
                break;
            case AniDbResponseCode.MultipleFilesFound:
                break;
            case AniDbResponseCode.FileInMyList:
                if (string.IsNullOrWhiteSpace(ResponseText))
                {
                    return;
                }
                var dataArr = ResponseText.Split('|');
                DateTimeOffset? watchedDate = dataArr[7] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[7])) : null;
                MyListResult = new AniDbMyListAddResult(int.Parse(dataArr[0]),
                    DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[5])),
                    Enum.Parse<MyListState>(dataArr[6]),
                    watchedDate is null ? false : true,
                    watchedDate,
                    Enum.Parse<MyListFileState>(dataArr[11])
                );
                break;
            case AniDbResponseCode.NoSuchFile:
                break;
            case AniDbResponseCode.NoSuchAnime:
                break;
            case AniDbResponseCode.NoSuchMyListEntry:
                break;
        }
    }
}
