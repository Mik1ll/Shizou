using System;
using System.Collections.Generic;
using System.Linq;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp.Results;

public sealed record AniDbAnimeResult
{
    public AniDbAnimeResult(string responseText, AMaskAnime mask)
    {
        var dataArr = responseText.TrimEnd().Split('|');
        var dataIdx = 0;
        foreach (var value in Enum.GetValues<AMaskAnime>().OrderByDescending(v => v))
            if (mask.HasFlag(value))
            {
                var data = dataArr[dataIdx++];
                if (string.IsNullOrWhiteSpace(data))
                    continue;

                // TODO: Test switch cases
                switch (value)
                {
                    case AMaskAnime.AnimeId:
                        AnimeId = int.Parse(data);
                        break;
                    case AMaskAnime.DateFlags:
                        DateInfo = Enum.Parse<DateFlagsAnime>(data);
                        break;
                    case AMaskAnime.Year:
                        Year = data;
                        break;
                    case AMaskAnime.Type:
                        Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                        break;
                    case AMaskAnime.RelatedAnimeIds:
                        RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                        break;
                    case AMaskAnime.RelatedAnimeTypes:
                        RelatedAnimeTypes =
                            data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x)).ToList();
                        break;
                    case AMaskAnime.TitleRomaji:
                        TitleRomaji = data;
                        break;
                    case AMaskAnime.TitleKanji:
                        TitleKanji = data;
                        break;
                    case AMaskAnime.TitleEnglish:
                        TitleEnglish = data;
                        break;
                    case AMaskAnime.TitlesOther:
                        TitlesOther = data.Split('\'').ToList();
                        break;
                    case AMaskAnime.TitlesShort:
                        TitlesShort = data.Split('\'').ToList();
                        break;
                    case AMaskAnime.TitlesSynonym:
                        TitlesSynonym = data.Split('\'').ToList();
                        break;
                    case AMaskAnime.TotalEpisodes:
                        TotalEpisodes = int.Parse(data);
                        break;
                    case AMaskAnime.HighestEpisodeNumber:
                        HighestEpisodeNumber = int.Parse(data);
                        break;
                    case AMaskAnime.SpecialEpisodeCount:
                        SpecialEpisodeCount = int.Parse(data);
                        break;
                    case AMaskAnime.AirDate:
                        AirDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data));
                        break;
                    case AMaskAnime.EndDate:
                        EndDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data));
                        break;
                    case AMaskAnime.Url:
                        Url = data;
                        break;
                    case AMaskAnime.PicName:
                        PicName = data;
                        break;
                    case AMaskAnime.Rating:
                        Rating = int.Parse(data);
                        break;
                    case AMaskAnime.VoteCount:
                        VoteCount = int.Parse(data);
                        break;
                    case AMaskAnime.TempRating:
                        TempRating = int.Parse(data);
                        break;
                    case AMaskAnime.TempVoteCount:
                        TempVoteCount = int.Parse(data);
                        break;
                    case AMaskAnime.AvgReviewRating:
                        AvgReviewRating = int.Parse(data);
                        break;
                    case AMaskAnime.ReviewCount:
                        ReviewCount = int.Parse(data);
                        break;
                    case AMaskAnime.Awards:
                        Awards = data.Split('\'').ToList();
                        break;
                    case AMaskAnime.IsRestricted:
                        IsRestricted = int.Parse(data) != 0;
                        break;
                    case AMaskAnime.AnnId:
                        AnnId = int.Parse(data);
                        break;
                    case AMaskAnime.AllCinemaId:
                        AllCinemaId = int.Parse(data);
                        break;
                    case AMaskAnime.AnimeNfoId:
                        AnimeNfoId = data;
                        break;
                    case AMaskAnime.TagNames:
                        TagNames = data.Split(',').ToList();
                        break;
                    case AMaskAnime.TagIds:
                        TagIds = data.Split(',').Select(x => int.Parse(x)).ToList();
                        break;
                    case AMaskAnime.TagWeights:
                        TagWeights = data.Split(',').Select(x => int.Parse(x)).ToList();
                        break;
                    case AMaskAnime.DateRecordUpdated:
                        DateRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data));
                        break;
                    case AMaskAnime.CharacterIds:
                        CharacterIds = data.Split(',').Select(x => int.Parse(x)).ToList();
                        break;
                    case AMaskAnime.SpecialsCount:
                        SpecialsCount = int.Parse(data);
                        break;
                    case AMaskAnime.CreditsCount:
                        CreditsCount = int.Parse(data);
                        break;
                    case AMaskAnime.OthersCount:
                        OthersCount = int.Parse(data);
                        break;
                    case AMaskAnime.TrailersCount:
                        TrailersCount = int.Parse(data);
                        break;
                    case AMaskAnime.ParodiesCount:
                        ParodiesCount = int.Parse(data);
                        break;
                }
            }
    }

    public int? AnimeId { get; }
    public DateFlagsAnime? DateInfo { get; }
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
    public DateTimeOffset? AirDate { get; }
    public DateTimeOffset? EndDate { get; }
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
    public DateTimeOffset? DateRecordUpdated { get; }
    public List<int>? CharacterIds { get; }
    public int? SpecialsCount { get; }
    public int? CreditsCount { get; }
    public int? OthersCount { get; }
    public int? TrailersCount { get; }
    public int? ParodiesCount { get; }
}
