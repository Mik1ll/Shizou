using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Udp.Results;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListAddRequest : AniDbUdpRequest
{
    public bool? Watched { get; set; }
    public DateTimeOffset? WatchedDate { get; set; }
    public MyListState? State { get; set; }
    public MyListFileState? FileState { get; set; }
    public AniDbMyListAddResult? MyListResult { get; private set; }

    public MyListAddRequest(
        ILogger<MyListAddRequest> logger, AniDbUdpState aniDbUdpState
    ) : base("MYLISTADD", logger, aniDbUdpState)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (string.IsNullOrWhiteSpace(ResponseText))
                {
                    return Task.CompletedTask;
                }
                if (Args["edit"] == "0" && Args.ContainsKey("fid"))
                    MyListResult = new AniDbMyListAddResult(int.Parse(ResponseText), DateTimeOffset.UtcNow, State, Watched, WatchedDate, FileState);
                break;
            case AniDbResponseCode.MyListEdited:
                break;
            case AniDbResponseCode.MultipleFilesFound:
                break;
            case AniDbResponseCode.FileInMyList:
                if (string.IsNullOrWhiteSpace(ResponseText))
                {
                    return Task.CompletedTask;
                }
                var dataArr = ResponseText.Split('|');
                DateTimeOffset? watchedDate = dataArr[7] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[7])) : null;
                MyListResult = new AniDbMyListAddResult(int.Parse(dataArr[0]),
                    DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[5])),
                    Enum.Parse<MyListState>(dataArr[6]),
                    watchedDate is not null,
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
        return Task.CompletedTask;
    }
}
