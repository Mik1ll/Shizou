using System;
using System.Collections.Generic;
using System.Linq;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public sealed record FileResult
{
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
                        GroupId = data != "0" ? int.Parse(data) : null;
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
                        AudioBitRates = data.Split('\'').Where(e => e != "none").Select(int.Parse).ToList();
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
                        RelatedAnimeIds = data.Split('\'').Select(int.Parse).ToList();
                        break;
                    case AMaskFile.RelatedAnimeTypes:
                        RelatedAnimeTypes =
                            data.Split('\'').Select(Enum.Parse<RelatedAnimeType>).ToList();
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

    public int FileId { get; }
    public int? AnimeId { get; }
    public int? EpisodeId { get; }
    public int? GroupId { get; }
    public int? MyListId { get; }
    public List<int>? OtherEpisodeIds { get; }
    public List<float>? OtherEpisodePercentages { get; }
    public bool? IsDeprecated { get; }
    public FileState? State { get; }

    public long? Size { get; }
    public string? Ed2K { get; }
    public string? Md5 { get; }
    public string? Sha1 { get; }
    public string? Crc32 { get; }
    public int? VideoColorDepth { get; }

    public string? Quality { get; }
    public string? Source { get; }
    public List<string>? AudioCodecs { get; }
    public List<int>? AudioBitRates { get; }
    public string? VideoCodec { get; }
    public int? VideoBitRate { get; }
    public string? VideoResolution { get; }
    public string? FileExtension { get; }

    public List<string>? DubLanguages { get; }
    public List<string>? SubLangugages { get; }
    public int? LengthInSeconds { get; }
    public string? Description { get; }
    public DateTimeOffset? EpisodeAiredDate { get; }
    public string? AniDbFileName { get; }

    public MyListState? MyListState { get; }
    public MyListFileState? MyListFileState { get; }
    public bool? MyListViewed { get; }
    public DateTimeOffset? MyListViewDate { get; }
    public string? MyListStorage { get; }
    public string? MyListSource { get; }
    public string? MyListOther { get; }

    #endregion FMask

    #region AMask

    public int? TotalEpisodes { get; }
    public int? HighestEpisodeNumber { get; }
    public string? Year { get; }
    public AnimeType? Type { get; }
    public List<int>? RelatedAnimeIds { get; }
    public List<RelatedAnimeType>? RelatedAnimeTypes { get; }
    public List<string>? Categories { get; }

    public string? TitleRomaji { get; }
    public string? TitleKanji { get; }
    public string? TitleEnglish { get; }
    public List<string>? TitlesOther { get; }
    public List<string>? TitlesShort { get; }
    public List<string>? TitlesSynonym { get; }

    public string? EpisodeNumber { get; }
    public string? EpisodeTitleEnglish { get; }
    public string? EpisodeTitleRomaji { get; }
    public string? EpisodeTitleKanji { get; }
    public int? EpisodeRating { get; }
    public int? EpisodeVoteCount { get; }

    public string? GroupName { get; }
    public string? GroupNameShort { get; }
    public DateTimeOffset? DateRecordUpdated { get; }

    #endregion AMask
}
