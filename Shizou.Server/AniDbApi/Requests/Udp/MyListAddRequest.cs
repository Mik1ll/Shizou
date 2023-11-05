using System;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListAddResponse : UdpResponse
{
    public MyListEntryResult? ExistingEntryResult { get; init; }
    public int? AddedEntryId { get; init; }
    public int? EntriesAffected { get; init; }
}

public class MyListAddRequest : AniDbUdpRequest<MyListAddResponse>, IMyListAddRequest
{
    public MyListAddRequest(ILogger<MyListAddRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("MYLISTADD", logger,
        aniDbUdpState, rateLimiter)
    {
    }

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

    protected override MyListAddResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        int? addedEntryId = null;
        int? entriesAffected = null;
        MyListEntryResult? existingEntryResult = null;
        switch (responseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (string.IsNullOrWhiteSpace(responseText))
                    break;
                if (Args.ContainsKey("fid") || Args.ContainsKey("ed2k"))
                {
                    addedEntryId = int.Parse(responseText);
                    entriesAffected = 1;
                }
                else
                {
                    entriesAffected = int.Parse(responseText);
                }

                break;
            case AniDbResponseCode.MyListEdited:
                entriesAffected = string.IsNullOrWhiteSpace(responseText) ? 1 : int.Parse(responseText);
                break;
            case AniDbResponseCode.MultipleFilesFound:
                // May only occur if using group name / gid?
                break;
            case AniDbResponseCode.FileInMyList:
                if (string.IsNullOrWhiteSpace(responseText))
                    break;
                var data = responseText.Split('|');
                existingEntryResult = new MyListEntryResult(data);
                break;
            case AniDbResponseCode.NoSuchFile:
                break;
            case AniDbResponseCode.NoSuchAnime:
                break;
            case AniDbResponseCode.NoSuchMyListEntry:
                break;
        }

        return new MyListAddResponse
        {
            ResponseText = responseText,
            ResponseCode = responseCode,
            ResponseCodeText = responseCodeText,
            ExistingEntryResult = existingEntryResult,
            AddedEntryId = addedEntryId,
            EntriesAffected = entriesAffected
        };
    }
}
