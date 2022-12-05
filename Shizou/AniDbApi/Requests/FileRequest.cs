using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Results;
using Shizou.CommandProcessors;

namespace Shizou.AniDbApi.Requests;

public sealed class FileRequest : AniDbUdpRequest
{
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

    [Flags]
    public enum AMask : uint
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

    public const AMask DefaultAMask = AMask.GroupName | AMask.GroupNameShort | AMask.DateAnimeRecordUpdated | AMask.TitleRomaji |
                                      AMask.EpisodeTitleEnglish |
                                      AMask.EpisodeNumber | AMask.TotalEpisodes | AMask.HighestEpisodeNumber | AMask.Type | AMask.EpisodeTitleRomaji |
                                      AMask.EpisodeTitleKanji;
    public const FMask DefaultFMask = FMask.Crc32 | FMask.Md5 | FMask.Sha1 | FMask.Size | FMask.Quality | FMask.Source | FMask.State | FMask.AnimeId |
                                      FMask.AudioCodecs | FMask.DubLanguages | FMask.Ed2K | FMask.EpisodeId | FMask.GroupId | FMask.IsDeprecated |
                                      FMask.OtherEpisodes | FMask.SubLangugages | FMask.VideoCodec | FMask.VideoResolution | FMask.AudioBitRates |
                                      FMask.EpisodeAiredDate | FMask.LengthInSeconds | FMask.MyListId | FMask.MyListOther | FMask.MyListSource |
                                      FMask.MyListState | FMask.MyListStorage | FMask.MyListViewed | FMask.MyListFileState | FMask.MyListViewDate |
                                      FMask.VideoBitRate | FMask.VideoColorDepth | FMask.AniDbFileName;
    private readonly AMask _aMask;
    private readonly FMask _fMask;

    private FileRequest(IServiceProvider provider, FMask fMask, AMask aMask) : base(provider.GetRequiredService<ILogger<FileRequest>>(),
        provider.GetRequiredService<AniDbUdp>(),
        provider.GetRequiredService<AniDbUdpProcessor>())
    {
        _fMask = fMask;
        _aMask = aMask;
        Params["fmask"] = ((ulong)fMask).ToString("X10");
        Params["amask"] = aMask.ToString("X");
    }

    public FileRequest(IServiceProvider provider, int fileId, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
    {
        Params["fid"] = fileId.ToString();
    }

    public FileRequest(IServiceProvider provider, long fileSize, string ed2K, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
    {
        Params["size"] = fileSize.ToString();
        Params["ed2k"] = ed2K;
    }

    // TODO: Test if epno can take special episode string
    public FileRequest(IServiceProvider provider, int animeId, int groupId, string episodeNumber, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
    {
        Params["aid"] = animeId.ToString();
        Params["gid"] = groupId.ToString();
        Params["epno"] = episodeNumber;
    }

    public AniDbFileResult? FileResult { get; private set; }
    public List<int>? MultipleFilesResult { get; private set; }

    public override string Command { get; } = "FILE";
    public override Dictionary<string, string> Params { get; } = new();

    public override async Task Process()
    {
        await SendRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.File:
                if (string.IsNullOrWhiteSpace(ResponseText))
                    Errored = true;
                else
                    FileResult = new AniDbFileResult(ResponseText, _fMask, _aMask);
                break;
            case AniDbResponseCode.MulitipleFilesFound:
                if (ResponseText is not null)
                    MultipleFilesResult = ResponseText.Split('|').Select(fid => int.Parse(fid)).ToList();
                break;
            case AniDbResponseCode.NoSuchFile:
                break;
        }
    }
}