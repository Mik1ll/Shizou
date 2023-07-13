using System;
using System.Collections.Generic;
using System.Linq;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public sealed record FileResult
{
    public FileResult()
    {
    }

    public FileResult(string responseText, FMask fMask, AMaskFile aMask)
    {
        var dataArr = responseText.TrimEnd().Split('|');
        var dataIdx = 0;
        FileId = int.Parse(dataArr[dataIdx++]);
        foreach (var value in Enum.GetValues<FMask>().OrderByDescending(v => v))
            if (fMask.HasFlag(value))
            {
                var data = dataArr[dataIdx++];
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                switch (value)
                {
                    case FMask.AnimeId:
                        AnimeId = int.Parse(data);
                        break;
                    case FMask.EpisodeId:
                        EpisodeId = int.Parse(data);
                        break;
                    case FMask.GroupId:
                        GroupId = int.Parse(data);
                        break;
                    case FMask.MyListId:
                        MyListId = data != "0" ? int.Parse(data) : null;
                        break;
                    case FMask.OtherEpisodes:
                        var otherEpisodes = data.Split('\'').Select(eps =>
                        {
                            var splitLine = eps.Split(',');
                            return (int.Parse(splitLine[0]), int.Parse(splitLine[1]) / 100f);
                        }).ToList();
                        OtherEpisodeIds = otherEpisodes.Select(e => e.Item1).ToList();
                        OtherEpisodePercentages = otherEpisodes.Select(e => e.Item2).ToList();
                        break;
                    case FMask.IsDeprecated:
                        IsDeprecated = int.Parse(data) != 0;
                        break;
                    case FMask.State:
                        State = Enum.Parse<FileState>(data);
                        break;

                    case FMask.Size:
                        Size = long.Parse(data);
                        break;
                    case FMask.Ed2K:
                        Ed2K = data;
                        break;
                    case FMask.Md5:
                        Md5 = data;
                        break;
                    case FMask.Sha1:
                        Sha1 = data;
                        break;
                    case FMask.Crc32:
                        Crc32 = data;
                        break;
                    case FMask.VideoColorDepth:
                        VideoColorDepth = int.Parse(data);
                        break;

                    case FMask.Quality:
                        Quality = data;
                        break;
                    case FMask.Source:
                        Source = data;
                        break;
                    case FMask.AudioCodecs:
                        AudioCodecs = data.Split('\'').Where(e => e != "none").ToList();
                        break;
                    case FMask.AudioBitRates:
                        AudioBitRates = data.Split('\'').Where(e => e != "none").Select(x => int.Parse(x)).ToList();
                        break;
                    case FMask.VideoCodec:
                        VideoCodec = data != "none" ? data : null;
                        break;
                    case FMask.VideoBitRate:
                        VideoBitRate = data != "none" ? int.Parse(data) : null;
                        break;
                    case FMask.VideoResolution:
                        VideoResolution = data != "none" ? data : null;
                        break;
                    case FMask.FileExtension:
                        FileExtension = data;
                        break;

                    case FMask.DubLanguages:
                        DubLanguages = data.Split('\'').Where(e => e != "none").ToList();
                        break;
                    case FMask.SubLangugages:
                        SubLangugages = data.Split('\'').Where(e => e != "none").ToList();
                        break;
                    case FMask.LengthInSeconds:
                        LengthInSeconds = data != "0" ? int.Parse(data) : null;
                        break;
                    case FMask.Description:
                        Description = AniDbUdpRequest.DataUnescape(data);
                        break;
                    case FMask.EpisodeAiredDate:
                        EpisodeAiredDate = data != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)) : null;
                        break;
                    case FMask.AniDbFileName:
                        AniDbFileName = data;
                        break;

                    case FMask.MyListState:
                        MyListState = Enum.Parse<MyListState>(data);
                        break;
                    case FMask.MyListFileState:
                        MyListFileState = Enum.Parse<MyListFileState>(data);
                        break;
                    case FMask.MyListViewed:
                        MyListViewed = int.Parse(data) != 0;
                        break;
                    case FMask.MyListViewDate:
                        MyListViewDate = data != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)) : null;
                        break;
                    case FMask.MyListStorage:
                        MyListStorage = data;
                        break;
                    case FMask.MyListSource:
                        MyListSource = data;
                        break;
                    case FMask.MyListOther:
                        MyListOther = data;
                        break;
                }
            }
        foreach (var value in Enum.GetValues<AMaskFile>().OrderByDescending(v => v))
            if (aMask.HasFlag(value))
            {
                var data = dataArr[dataIdx++];
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                switch (value)
                {
                    case AMaskFile.TotalEpisodes:
                        TotalEpisodes = int.Parse(data);
                        break;
                    case AMaskFile.HighestEpisodeNumber:
                        HighestEpisodeNumber = int.Parse(data);
                        break;
                    case AMaskFile.Year:
                        Year = data;
                        break;
                    case AMaskFile.Type:
                        Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                        break;
                    case AMaskFile.RelatedAnimeIds:
                        RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                        break;
                    case AMaskFile.RelatedAnimeTypes:
                        RelatedAnimeTypes =
                            data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x)).ToList();
                        break;
                    case AMaskFile.Categories:
                        Categories = data.Split(',').ToList();
                        break;

                    case AMaskFile.TitleRomaji:
                        TitleRomaji = data;
                        break;
                    case AMaskFile.TitleKanji:
                        TitleKanji = data;
                        break;
                    case AMaskFile.TitleEnglish:
                        TitleEnglish = data;
                        break;
                    case AMaskFile.TitlesOther:
                        TitlesOther = data.Split('\'').ToList();
                        break;
                    case AMaskFile.TitlesShort:
                        TitlesShort = data.Split('\'').ToList();
                        break;
                    case AMaskFile.TitlesSynonym:
                        TitlesSynonym = data.Split('\'').ToList();
                        break;

                    case AMaskFile.EpisodeNumber:
                        EpisodeNumber = data;
                        break;
                    case AMaskFile.EpisodeTitleEnglish:
                        EpisodeTitleEnglish = data;
                        break;
                    case AMaskFile.EpisodeTitleRomaji:
                        EpisodeTitleRomaji = data;
                        break;
                    case AMaskFile.EpisodeTitleKanji:
                        EpisodeTitleKanji = data;
                        break;
                    case AMaskFile.EpisodeRating:
                        EpisodeRating = int.Parse(data);
                        break;
                    case AMaskFile.EpisodeVoteCount:
                        EpisodeVoteCount = int.Parse(data);
                        break;

                    case AMaskFile.GroupName:
                        GroupName = data;
                        break;
                    case AMaskFile.GroupNameShort:
                        GroupNameShort = data;
                        break;
                    case AMaskFile.DateAnimeRecordUpdated:
                        DateRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data));
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
    public DateTimeOffset? EpisodeAiredDate { get; init; }
    public string? AniDbFileName { get; init; }

    public MyListState? MyListState { get; init; }
    public MyListFileState? MyListFileState { get; init; }
    public bool? MyListViewed { get; init; }
    public DateTimeOffset? MyListViewDate { get; init; }
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
    public DateTimeOffset? DateRecordUpdated { get; init; }

    #endregion AMask
}
