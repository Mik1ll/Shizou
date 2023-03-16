using System;
using System.Collections.Generic;
using System.Linq;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests.Udp.Results;

public sealed record AniDbFileResult
{
    public AniDbFileResult()
    {
    }

    public AniDbFileResult(string responseText, FileRequest.FMask fMask, FileRequest.AMask aMask)
    {
        var dataArr = responseText.TrimEnd().Split('|');
        var dataIdx = 0;
        FileId = int.Parse(dataArr[dataIdx++]);
        foreach (var value in Enum.GetValues<FileRequest.FMask>().OrderByDescending(v => v))
            if (fMask.HasFlag(value))
            {
                var data = dataArr[dataIdx++];
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                switch (value)
                {
                    case FileRequest.FMask.AnimeId:
                        AnimeId = int.Parse(data);
                        break;
                    case FileRequest.FMask.EpisodeId:
                        EpisodeId = int.Parse(data);
                        break;
                    case FileRequest.FMask.GroupId:
                        GroupId = int.Parse(data);
                        break;
                    case FileRequest.FMask.MyListId:
                        MyListId = data != "0" ? int.Parse(data) : null;
                        break;
                    case FileRequest.FMask.OtherEpisodes:
                        var otherEpisodes = data.Split('\'').Select(eps =>
                        {
                            var splitLine = eps.Split(',');
                            return (int.Parse(splitLine[0]), int.Parse(splitLine[1]) / 100f);
                        }).ToList();
                        OtherEpisodeIds = otherEpisodes.Select(e => e.Item1).ToList();
                        OtherEpisodePercentages = otherEpisodes.Select(e => e.Item2).ToList();
                        break;
                    case FileRequest.FMask.IsDeprecated:
                        IsDeprecated = int.Parse(data) != 0;
                        break;
                    case FileRequest.FMask.State:
                        State = Enum.Parse<FileState>(data);
                        break;

                    case FileRequest.FMask.Size:
                        Size = long.Parse(data);
                        break;
                    case FileRequest.FMask.Ed2K:
                        Ed2K = data;
                        break;
                    case FileRequest.FMask.Md5:
                        Md5 = data;
                        break;
                    case FileRequest.FMask.Sha1:
                        Sha1 = data;
                        break;
                    case FileRequest.FMask.Crc32:
                        Crc32 = data;
                        break;
                    case FileRequest.FMask.VideoColorDepth:
                        VideoColorDepth = int.Parse(data);
                        break;

                    case FileRequest.FMask.Quality:
                        Quality = data;
                        break;
                    case FileRequest.FMask.Source:
                        Source = data;
                        break;
                    case FileRequest.FMask.AudioCodecs:
                        AudioCodecs = data.Split('\'').Where(e => e != "none").ToList();
                        break;
                    case FileRequest.FMask.AudioBitRates:
                        AudioBitRates = data.Split('\'').Where(e => e != "none").Select(x => int.Parse(x)).ToList();
                        break;
                    case FileRequest.FMask.VideoCodec:
                        VideoCodec = data != "none" ? data : null;
                        break;
                    case FileRequest.FMask.VideoBitRate:
                        VideoBitRate = data != "none" ? int.Parse(data) : null;
                        break;
                    case FileRequest.FMask.VideoResolution:
                        VideoResolution = data != "none" ? data : null;
                        break;
                    case FileRequest.FMask.FileExtension:
                        FileExtension = data;
                        break;

                    case FileRequest.FMask.DubLanguages:
                        DubLanguages = data.Split('\'').Where(e => e != "none").ToList();
                        break;
                    case FileRequest.FMask.SubLangugages:
                        SubLangugages = data.Split('\'').Where(e => e != "none").ToList();
                        break;
                    case FileRequest.FMask.LengthInSeconds:
                        LengthInSeconds = data != "0" ? int.Parse(data) : null;
                        break;
                    case FileRequest.FMask.Description:
                        Description = AniDbUdpRequest.DataUnescape(data);
                        break;
                    case FileRequest.FMask.EpisodeAiredDate:
                        EpisodeAiredDate = data != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime : null;
                        break;
                    case FileRequest.FMask.AniDbFileName:
                        AniDbFileName = data;
                        break;

                    case FileRequest.FMask.MyListState:
                        MyListState = Enum.Parse<MyListState>(data);
                        break;
                    case FileRequest.FMask.MyListFileState:
                        MyListFileState = Enum.Parse<MyListFileState>(data);
                        break;
                    case FileRequest.FMask.MyListViewed:
                        MyListViewed = int.Parse(data) != 0;
                        break;
                    case FileRequest.FMask.MyListViewDate:
                        MyListViewDate = data != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime : null;
                        break;
                    case FileRequest.FMask.MyListStorage:
                        MyListStorage = data;
                        break;
                    case FileRequest.FMask.MyListSource:
                        MyListSource = data;
                        break;
                    case FileRequest.FMask.MyListOther:
                        MyListOther = data;
                        break;
                }
            }
        foreach (var value in Enum.GetValues<FileRequest.AMask>().OrderByDescending(v => v))
            if (aMask.HasFlag(value))
            {
                var data = dataArr[dataIdx++];
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                switch (value)
                {
                    case FileRequest.AMask.TotalEpisodes:
                        TotalEpisodes = int.Parse(data);
                        break;
                    case FileRequest.AMask.HighestEpisodeNumber:
                        HighestEpisodeNumber = int.Parse(data);
                        break;
                    case FileRequest.AMask.Year:
                        Year = data;
                        break;
                    case FileRequest.AMask.Type:
                        Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                        break;
                    case FileRequest.AMask.RelatedAnimeIds:
                        RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                        break;
                    case FileRequest.AMask.RelatedAnimeTypes:
                        RelatedAnimeTypes =
                            data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x)).ToList();
                        break;
                    case FileRequest.AMask.Categories:
                        Categories = data.Split(',').ToList();
                        break;

                    case FileRequest.AMask.TitleRomaji:
                        TitleRomaji = data;
                        break;
                    case FileRequest.AMask.TitleKanji:
                        TitleKanji = data;
                        break;
                    case FileRequest.AMask.TitleEnglish:
                        TitleEnglish = data;
                        break;
                    case FileRequest.AMask.TitlesOther:
                        TitlesOther = data.Split('\'').ToList();
                        break;
                    case FileRequest.AMask.TitlesShort:
                        TitlesShort = data.Split('\'').ToList();
                        break;
                    case FileRequest.AMask.TitlesSynonym:
                        TitlesSynonym = data.Split('\'').ToList();
                        break;

                    case FileRequest.AMask.EpisodeNumber:
                        EpisodeNumber = data;
                        break;
                    case FileRequest.AMask.EpisodeTitleEnglish:
                        EpisodeTitleEnglish = data;
                        break;
                    case FileRequest.AMask.EpisodeTitleRomaji:
                        EpisodeTitleRomaji = data;
                        break;
                    case FileRequest.AMask.EpisodeTitleKanji:
                        EpisodeTitleKanji = data;
                        break;
                    case FileRequest.AMask.EpisodeRating:
                        EpisodeRating = int.Parse(data);
                        break;
                    case FileRequest.AMask.EpisodeVoteCount:
                        EpisodeVoteCount = int.Parse(data);
                        break;

                    case FileRequest.AMask.GroupName:
                        GroupName = data;
                        break;
                    case FileRequest.AMask.GroupNameShort:
                        GroupNameShort = data;
                        break;
                    case FileRequest.AMask.DateAnimeRecordUpdated:
                        DateRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                        break;
                }
            }
    }

    #region FMask

    public int FileId { get; init; }
    public int? AnimeId { get; init; }
    public int? EpisodeId { get; init; }
    public int? GroupId { get; init; }
    public int? MyListId { get; init; }
    public List<int>? OtherEpisodeIds { get; init; }
    public List<float>? OtherEpisodePercentages { get; init; }
    public bool? IsDeprecated { get; init; }
    public FileState? State { get; init; }

    public long? Size { get; init; }
    public string? Ed2K { get; init; }
    public string? Md5 { get; init; }
    public string? Sha1 { get; init; }
    public string? Crc32 { get; init; }
    public int? VideoColorDepth { get; init; }

    public string? Quality { get; init; }
    public string? Source { get; init; }
    public List<string>? AudioCodecs { get; init; }
    public List<int>? AudioBitRates { get; init; }
    public string? VideoCodec { get; init; }
    public int? VideoBitRate { get; init; }
    public string? VideoResolution { get; init; }
    public string? FileExtension { get; init; }

    public List<string>? DubLanguages { get; init; }
    public List<string>? SubLangugages { get; init; }
    public int? LengthInSeconds { get; init; }
    public string? Description { get; init; }
    public DateTime? EpisodeAiredDate { get; init; }
    public string? AniDbFileName { get; init; }

    public MyListState? MyListState { get; init; }
    public MyListFileState? MyListFileState { get; init; }
    public bool? MyListViewed { get; init; }
    public DateTime? MyListViewDate { get; init; }
    public string? MyListStorage { get; init; }
    public string? MyListSource { get; init; }
    public string? MyListOther { get; init; }

    #endregion FMask

    #region AMask

    public int? TotalEpisodes { get; init; }
    public int? HighestEpisodeNumber { get; init; }
    public string? Year { get; init; }
    public AnimeType? Type { get; init; }
    public List<int>? RelatedAnimeIds { get; init; }
    public List<RelatedAnimeType>? RelatedAnimeTypes { get; init; }
    public List<string>? Categories { get; init; }

    public string? TitleRomaji { get; init; }
    public string? TitleKanji { get; init; }
    public string? TitleEnglish { get; init; }
    public List<string>? TitlesOther { get; init; }
    public List<string>? TitlesShort { get; init; }
    public List<string>? TitlesSynonym { get; init; }

    public string? EpisodeNumber { get; init; }
    public string? EpisodeTitleEnglish { get; init; }
    public string? EpisodeTitleRomaji { get; init; }
    public string? EpisodeTitleKanji { get; init; }
    public int? EpisodeRating { get; init; }
    public int? EpisodeVoteCount { get; init; }

    public string? GroupName { get; init; }
    public string? GroupNameShort { get; init; }
    public DateTime? DateRecordUpdated { get; init; }

    #endregion AMask
}
