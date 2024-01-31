using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class FileResponse : UdpResponse
{
    public FileResult? FileResult { get; init; }
    public List<int>? MultipleFilesResult { get; init; }
}

public class FileRequest : AniDbUdpRequest<FileResponse>, IFileRequest
{
    private readonly AMaskFile _aMask = FileResult.ResultAMaskFile;
    private readonly FMask _fMask = FileResult.ResultFMask;

    public FileRequest(ILogger<FileRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("FILE", logger, aniDbUdpState, rateLimiter)
    {
    }

    public void SetParameters(int fileId)
    {
        SetParameters();
        Args["fid"] = fileId.ToString();
        ParametersSet = true;
    }

    public void SetParameters(long fileSize, string ed2K)
    {
        SetParameters();
        Args["size"] = fileSize.ToString();
        Args["ed2k"] = ed2K;
        ParametersSet = true;
    }

    // TODO: Test if epno can take special episode string
    public void SetParameters(int animeId, int groupId, string episodeNumber)
    {
        SetParameters();
        Args["aid"] = animeId.ToString();
        Args["gid"] = groupId.ToString();
        Args["epno"] = episodeNumber;
        ParametersSet = true;
    }

    protected override FileResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        FileResult? fileResult = null;
        List<int>? multipleFilesResult = null;
        switch (responseCode)
        {
            case AniDbResponseCode.File:
                if (!string.IsNullOrWhiteSpace(responseText))
                    fileResult = new FileResult(responseText);
                break;
            case AniDbResponseCode.MultipleFilesFound:
                multipleFilesResult = responseText.Split('|').Select(int.Parse).ToList();
                break;
            case AniDbResponseCode.NoSuchFile:
                break;
        }

        return new FileResponse
        {
            ResponseText = responseText,
            ResponseCode = responseCode,
            ResponseCodeText = responseCodeText,
            FileResult = fileResult,
            MultipleFilesResult = multipleFilesResult
        };
    }

    private void SetParameters()
    {
        Args["fmask"] = ((ulong)_fMask).ToString("X10");
        Args["amask"] = _aMask.ToString("X");
    }
}

// @formatter:off
[Flags]
public enum AMaskFile : uint
{
    TotalEpisodes = 1u << 31,
    HighestEpisodeNumber = 1 << 30,
    Year = 1 << 29,
    Type = 1 << 28,
    RelatedAnimeIds = 1 << 27,
    RelatedAnimeTypes = 1 << 26,
    Categories = 1 << 25,
    // Unused = 1 << 24,

    TitleRomaji = 1 << 23,
    TitleKanji = 1 << 22,
    TitleEnglish = 1 << 21,
    TitlesOther = 1 << 20,
    TitlesShort = 1 << 19,
    TitlesSynonym = 1 << 18,
    // Unused = 1 << 17,
    // Unused = 1 << 16,

    EpisodeNumber = 1 << 15,
    EpisodeTitleEnglish = 1 << 14,
    EpisodeTitleRomaji = 1 << 13,
    EpisodeTitleKanji = 1 << 12,
    EpisodeRating = 1 << 11,
    EpisodeVoteCount = 1 << 10,
    // Unused = 1 << 9,
    // Unused - 1 << 8,

    GroupName = 1 << 7,
    GroupNameShort = 1 << 6,
    // Unused = 1 << 5,
    // Unused = 1 << 4,
    // Unused = 1 << 3,
    // Unused = 1 << 2,
    // Unused = 1 << 1,
    DateAnimeRecordUpdated = 1 << 0
}

[Flags]
public enum FMask : ulong
{
    // Unused = 1ul << 39,
    AnimeId = 1ul << 38,
    EpisodeId = 1ul << 37,
    GroupId = 1ul << 36,
    MyListId = 1ul << 35,
    OtherEpisodes = 1ul << 34,
    IsDeprecated = 1ul << 33,
    State = 1ul << 32,

    Size = 1u << 31,
    Ed2K = 1 << 30,
    Md5 = 1 << 29,
    Sha1 = 1 << 28,
    Crc32 = 1 << 27,
    // Unused = 1 << 26,
    VideoColorDepth = 1 << 25,
    // Unused = 1 << 24,

    Quality = 1 << 23,
    Source = 1 << 22,
    AudioCodecs = 1 << 21,
    AudioBitRates = 1 << 20,
    VideoCodec = 1 << 19,
    VideoBitRate = 1 << 18,
    VideoResolution = 1 << 17,
    FileExtension = 1 << 16,

    DubLanguages = 1 << 15,
    SubLangugages = 1 << 14,
    LengthInSeconds = 1 << 13,
    Description = 1 << 12,
    EpisodeAiredDate = 1 << 11,
    // Unused = 1 << 10,
    // Unused = 1 << 9,
    AniDbFileName = 1 << 8,

    MyListState = 1 << 7,
    MyListFileState = 1 << 6,
    MyListViewed = 1 << 5,
    MyListViewDate = 1 << 4,
    MyListStorage = 1 << 3,
    MyListSource = 1 << 2,
    MyListOther = 1 << 1
    // Unused = 1 << 0,
}
// @formatter:on
