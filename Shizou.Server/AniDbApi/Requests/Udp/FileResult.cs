using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Shizou.Data.Enums;
using Shizou.Data.Extensions;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public sealed record FileResult
{
    public const AMaskFile ResultAMaskFile = AMaskFile.GroupName | AMaskFile.GroupNameShort;

    public const FMask ResultFMask = FMask.AnimeId | FMask.EpisodeId | FMask.GroupId | FMask.MyListId | FMask.OtherEpisodes | FMask.IsDeprecated |
                                     FMask.State |
                                     FMask.Size | FMask.Ed2K | FMask.Md5 | FMask.Sha1 | FMask.Crc32 | FMask.VideoColorDepth |
                                     FMask.Source | FMask.AudioCodecs | FMask.AudioBitRates | FMask.VideoCodec | FMask.VideoBitRate | FMask.VideoResolution |
                                     FMask.DubLanguages | FMask.SubLangugages | FMask.LengthInSeconds | FMask.AniDbFileName |
                                     FMask.MyListState | FMask.MyListFileState | FMask.MyListViewed | FMask.MyListViewDate | FMask.MyListStorage |
                                     FMask.MyListSource | FMask.MyListOther;

    public FileResult(string responseText)
    {
        var dataArr = responseText.TrimEnd().Split('|');
        var dataIdx = 0;
        FileId = int.Parse(dataArr[dataIdx]);
        AnimeId = int.Parse(dataArr[++dataIdx]);
        EpisodeId = int.Parse(dataArr[++dataIdx]);
        GroupId = dataArr[++dataIdx] != "0" ? int.Parse(dataArr[dataIdx]) : null;
        MyListId = dataArr[++dataIdx] != "0" ? int.Parse(dataArr[dataIdx]) : null;
        var otherEpisodes = dataArr[++dataIdx].Split('\'', StringSplitOptions.RemoveEmptyEntries).Select(eps =>
        {
            var splitLine = eps.Split(',');
            return (int.Parse(splitLine[0]), int.Parse(splitLine[1]) / 100f);
        }).ToList();
        OtherEpisodeIds = otherEpisodes.Select(e => e.Item1).ToList();
        OtherEpisodePercentages = otherEpisodes.Select(e => e.Item2).ToList();
        IsDeprecated = dataArr[++dataIdx] != "0";
        State = Enum.Parse<FileState>(dataArr[++dataIdx]);
        Size = long.Parse(dataArr[++dataIdx]);
        Ed2K = dataArr[++dataIdx].NullIfWhitespace();
        Md5 = dataArr[++dataIdx].NullIfWhitespace();
        Sha1 = dataArr[++dataIdx].NullIfWhitespace();
        Crc32 = dataArr[++dataIdx].NullIfWhitespace();
        VideoColorDepth = dataArr[++dataIdx] != string.Empty ? int.Parse(dataArr[dataIdx]) : null;
        Source = dataArr[++dataIdx].NullIfWhitespace();
        AudioCodecs = dataArr[++dataIdx].Split('\'', StringSplitOptions.RemoveEmptyEntries).Where(e => e != "none").ToList();
        AudioBitRates = dataArr[++dataIdx].Split('\'', StringSplitOptions.RemoveEmptyEntries).Where(e => e != "none").Select(int.Parse).ToList();
        VideoCodec = dataArr[++dataIdx] != "none" ? dataArr[dataIdx] : null;
        VideoBitRate = dataArr[++dataIdx] != "none" ? int.Parse(dataArr[dataIdx]) : null;
        VideoResolution = dataArr[++dataIdx] != "none" ? dataArr[dataIdx] : null;
        DubLanguages = dataArr[++dataIdx].Split('\'', StringSplitOptions.RemoveEmptyEntries).Where(e => e != "none").ToList();
        SubLangugages = dataArr[++dataIdx].Split('\'', StringSplitOptions.RemoveEmptyEntries).Where(e => e != "none").ToList();
        LengthInSeconds = dataArr[++dataIdx] != "0" ? int.Parse(dataArr[dataIdx]) : null;
        AniDbFileName = dataArr[++dataIdx];
        if (string.IsNullOrWhiteSpace(dataArr[++dataIdx]))
        {
            dataIdx += 6;
        }
        else
        {
            MyListState = Enum.Parse<MyListState>(dataArr[dataIdx]);
            MyListFileState = Enum.Parse<MyListFileState>(dataArr[++dataIdx]);
            MyListViewed = dataArr[++dataIdx] != "0";
            MyListViewDate = dataArr[++dataIdx] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[dataIdx])) : null;
            MyListStorage = dataArr[++dataIdx].NullIfWhitespace();
            MyListSource = dataArr[++dataIdx].NullIfWhitespace();
            MyListOther = dataArr[++dataIdx].NullIfWhitespace();
        }

        GroupName = dataArr[++dataIdx].NullIfWhitespace();
        // There is one group with an empty short name https://anidb.net/group/13019
        GroupNameShort = dataArr[++dataIdx];
    }

    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private FileResult()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    #region FMask

    public int FileId { get; init; }
    public int AnimeId { get; init; }
    public int EpisodeId { get; init; }
    public int? GroupId { get; init; }
    public int? MyListId { get; init; }
    public List<int> OtherEpisodeIds { get; init; }
    public List<float> OtherEpisodePercentages { get; init; }
    public bool IsDeprecated { get; init; }
    public FileState State { get; init; }
    public long Size { get; init; }
    public string? Ed2K { get; init; }
    public string? Md5 { get; init; }
    public string? Sha1 { get; init; }
    public string? Crc32 { get; init; }
    public int? VideoColorDepth { get; init; }
    public string? Source { get; init; }
    public List<string> AudioCodecs { get; init; }
    public List<int> AudioBitRates { get; init; }
    public string? VideoCodec { get; init; }
    public int? VideoBitRate { get; init; }
    public string? VideoResolution { get; init; }
    public List<string> DubLanguages { get; init; }
    public List<string> SubLangugages { get; init; }
    public int? LengthInSeconds { get; init; }
    public string AniDbFileName { get; init; }
    public MyListState? MyListState { get; init; }
    public MyListFileState? MyListFileState { get; init; }
    public bool? MyListViewed { get; init; }
    public DateTimeOffset? MyListViewDate { get; init; }
    public string? MyListStorage { get; init; }
    public string? MyListSource { get; init; }
    public string? MyListOther { get; init; }

    #endregion

    #region AMask

    public string? GroupName { get; init; }
    public string? GroupNameShort { get; init; }

    #endregion
}
