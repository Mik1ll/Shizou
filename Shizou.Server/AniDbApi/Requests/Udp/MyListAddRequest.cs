using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListAddRequest : AniDbUdpRequest, IMyListAddRequest
{
    public MyListAddRequest(ILogger<MyListAddRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("MYLISTADD", logger,
        aniDbUdpState, rateLimiter)
    {
    }

    public MyListEntryResult? ExistingEntryResult { get; private set; }
    public int? AddedEntryId { get; private set; }
    public int EntriesAffected { get; private set; }

    public void SetParameters(
        int fid, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    )
    {
        SetParameters(edit, watched, watchedDate, state);
        Args["fid"] = fid.ToString();
        ParametersSet = true;
    }

    public void SetParameters(
        int lid, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    )
    {
        SetParameters(true, watched, watchedDate, state);
        Args["lid"] = lid.ToString();
        ParametersSet = true;
    }

    public void SetParameters(
        int aid, string epno, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    )
    {
        SetParameters(edit, watched, watchedDate, state);
        Args["aid"] = aid.ToString();
        Args["epno"] = epno;
        Args["generic"] = "1";
        ParametersSet = true;
    }


    private void SetParameters(
        bool edit, bool? watched, DateTimeOffset? watchedDate, MyListState? state
    )
    {
        Args["filestate"] = ((int)MyListFileState.Normal).ToString();
        Args["edit"] = edit ? "1" : "0";
        if (watched is not null)
            Args["viewed"] = watched.Value ? "1" : "0";
        if (watchedDate is not null)
            Args["viewdate"] = watchedDate.Value.ToUnixTimeSeconds().ToString();
        if (state is not null)
            Args["state"] = ((int)state).ToString();
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
                {
                    EntriesAffected = int.Parse(ResponseText);
                }

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