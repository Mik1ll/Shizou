using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListAddRequest : AniDbUdpRequest
{
    public MyListEntryResult? ExistingEntryResult { get; private set; }
    public int? AddedEntryId { get; private set; }
    public int EntriesAffected { get; private set; }

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
                    return Task.CompletedTask;
                if (Args.ContainsKey("fid") || Args.ContainsKey("ed2k"))
                {
                    AddedEntryId = int.Parse(ResponseText);
                    EntriesAffected = 1;
                }
                else
                    EntriesAffected = int.Parse(ResponseText);
                break;
            case AniDbResponseCode.MyListEdited:
                EntriesAffected = string.IsNullOrWhiteSpace(ResponseText) ? 1 : int.Parse(ResponseText);
                break;
            case AniDbResponseCode.MultipleFilesFound:
                // May only occur if using group name / gid?
                break;
            case AniDbResponseCode.FileInMyList:
                if (string.IsNullOrWhiteSpace(ResponseText))
                    return Task.CompletedTask;
                var data = ResponseText.Split('|');
                ExistingEntryResult = new MyListEntryResult(data);
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
