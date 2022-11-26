using System;
using System.Collections.Generic;
using System.Linq;
using Shizou.AniDbApi.Requests;
using Shizou.Enums;

namespace Shizou.AniDbApi.Results;

public sealed record AniDbAnimeResult
{
    public AniDbAnimeResult(string responseText, AnimeRequest.AMask mask)
    {
        var dataArr = responseText.TrimEnd().Split('|');
        var dataIdx = 0;
        foreach (var value in Enum.GetValues<AnimeRequest.AMask>().OrderByDescending(v => v))
            if (mask.HasFlag(value))
            {
                var data = dataArr[dataIdx++];
                if (string.IsNullOrWhiteSpace(data))
                    continue;

                // TODO: Test switch cases
                switch (value)
                {
                    case AnimeRequest.AMask.AnimeId:
                        AnimeId = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.DateFlags:
                        DateInfo = Enum.Parse<AnimeRequest.DateFlags>(data);
                        break;
                    case AnimeRequest.AMask.Year:
                        Year = data;
                        break;
                    case AnimeRequest.AMask.Type:
                        Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                        break;
                    case AnimeRequest.AMask.RelatedAnimeIds:
                        RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                        break;
                    case AnimeRequest.AMask.RelatedAnimeTypes:
                        RelatedAnimeTypes =
                            data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x)).ToList();
                        break;
                    case AnimeRequest.AMask.TitleRomaji:
                        TitleRomaji = data;
                        break;
                    case AnimeRequest.AMask.TitleKanji:
                        TitleKanji = data;
                        break;
                    case AnimeRequest.AMask.TitleEnglish:
                        TitleEnglish = data;
                        break;
                    case AnimeRequest.AMask.TitlesOther:
                        TitlesOther = data.Split('\'').ToList();
                        break;
                    case AnimeRequest.AMask.TitlesShort:
                        TitlesShort = data.Split('\'').ToList();
                        break;
                    case AnimeRequest.AMask.TitlesSynonym:
                        TitlesSynonym = data.Split('\'').ToList();
                        break;
                    case AnimeRequest.AMask.TotalEpisodes:
                        TotalEpisodes = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.HighestEpisodeNumber:
                        HighestEpisodeNumber = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.SpecialEpisodeCount:
                        SpecialEpisodeCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.AirDate:
                        AirDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                        break;
                    case AnimeRequest.AMask.EndDate:
                        EndDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                        break;
                    case AnimeRequest.AMask.Url:
                        Url = data;
                        break;
                    case AnimeRequest.AMask.PicName:
                        PicName = data;
                        break;
                    case AnimeRequest.AMask.Rating:
                        Rating = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.VoteCount:
                        VoteCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.TempRating:
                        TempRating = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.TempVoteCount:
                        TempVoteCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.AvgReviewRating:
                        AvgReviewRating = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.ReviewCount:
                        ReviewCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.Awards:
                        Awards = data.Split('\'').ToList();
                        break;
                    case AnimeRequest.AMask.IsRestricted:
                        IsRestricted = int.Parse(data) != 0;
                        break;
                    case AnimeRequest.AMask.AnnId:
                        AnnId = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.AllCinemaId:
                        AllCinemaId = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.AnimeNfoId:
                        AnimeNfoId = data;
                        break;
                    case AnimeRequest.AMask.TagNames:
                        TagNames = data.Split(',').ToList();
                        break;
                    case AnimeRequest.AMask.TagIds:
                        TagIds = data.Split(',').Select(x => int.Parse(x)).ToList();
                        break;
                    case AnimeRequest.AMask.TagWeights:
                        TagWeights = data.Split(',').Select(x => int.Parse(x)).ToList();
                        break;
                    case AnimeRequest.AMask.DateRecordUpdated:
                        DateRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                        break;
                    case AnimeRequest.AMask.CharacterIds:
                        CharacterIds = data.Split(',').Select(x => int.Parse(x)).ToList();
                        break;
                    case AnimeRequest.AMask.SpecialsCount:
                        SpecialsCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.CreditsCount:
                        CreditsCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.OthersCount:
                        OthersCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.TrailersCount:
                        TrailersCount = int.Parse(data);
                        break;
                    case AnimeRequest.AMask.ParodiesCount:
                        ParodiesCount = int.Parse(data);
                        break;
                }
            }
    }

    public int? AnimeId { get; }
    public AnimeRequest.DateFlags? DateInfo { get; }
    public string? Year { get; }
    public AnimeType? Type { get; }
    public List<int>? RelatedAnimeIds { get; }
    public List<RelatedAnimeType>? RelatedAnimeTypes { get; }
    public string? TitleRomaji { get; }
    public string? TitleKanji { get; }
    public string? TitleEnglish { get; }
    public List<string>? TitlesOther { get; }
    public List<string>? TitlesShort { get; }
    public List<string>? TitlesSynonym { get; }
    public int? TotalEpisodes { get; }
    public int? HighestEpisodeNumber { get; }
    public int? SpecialEpisodeCount { get; }
    public DateTime? AirDate { get; }
    public DateTime? EndDate { get; }
    public string? Url { get; }
    public string? PicName { get; }
    public int? Rating { get; }
    public int? VoteCount { get; }
    public int? TempRating { get; }
    public int? TempVoteCount { get; }
    public int? AvgReviewRating { get; }
    public int? ReviewCount { get; }
    public List<string>? Awards { get; }
    public bool? IsRestricted { get; }
    public int? AnnId { get; }
    public int? AllCinemaId { get; }
    public string? AnimeNfoId { get; }
    public List<string>? TagNames { get; }
    public List<int>? TagIds { get; }
    public List<int>? TagWeights { get; }
    public DateTime? DateRecordUpdated { get; }
    public List<int>? CharacterIds { get; }
    public int? SpecialsCount { get; }
    public int? CreditsCount { get; }
    public int? OthersCount { get; }
    public int? TrailersCount { get; }
    public int? ParodiesCount { get; }
}
