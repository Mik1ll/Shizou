using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests
{
    [Flags]
    public enum DateFlags
    {
        UnkStartDay = 1 << 0,
        UnkStartMonth = 1 << 1,
        UnkEndDay = 1 << 2,
        UnkEndMonth = 1 << 3,
        AnimeEnded = 1 << 4,
        UnkStartYear = 1 << 5,
        UnkEndYear = 1 << 6
    }

    public class AnimeResult
    {
        public int? AnimeId { get; set; }
        public DateFlags? DateFlags { get; set; }
        public int? Year { get; set; }
        public AnimeType Type { get; set; }
        public List<int>? RelatedAnimeIds { get; set; }
        public List<RelatedAnimeType>? RelatedAnimeTypes { get; set; }
        public string? TitleRomaji { get; set; }
        public string? TitleKanji { get; set; }
        public string? TitleEnglish { get; set; }
        public List<string>? TitlesOther { get; set; }
        public List<string>? TitlesShort { get; set; }
        public List<string>? TitlesSynonym { get; set; }
        public int? TotalEpisodes { get; set; }
        public int? HighestEpisodeNumber { get; set; }
        public int? SpecialEpisodeCount { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Url { get; set; }
        public string? PicName { get; set; }
        public int? Rating { get; set; }
        public int? VoteCount { get; set; }
        public int? TempRating { get; set; }
        public int? TempVoteCount { get; set; }
        public int? AvgReviewRating { get; set; }
        public int? ReviewCount { get; set; }
        public List<string>? Awards { get; set; }
        public bool? IsRestricted { get; set; }
        public int? AnnId { get; set; }
        public int? AllCinemaId { get; set; }
        public string? AnimeNfoId { get; set; }
        public List<string>? TagNames { get; set; }
        public List<int>? TagIds { get; set; }
        public List<int>? TagWeights { get; set; }
        public DateTime? DateRecordUpdated { get; set; }
        public List<int>? CharacterIds { get; set; }
        public int? SpecialsCount { get; set; }
        public int? CreditsCount { get; set; }
        public int? OthersCount { get; set; }
        public int? TrailersCount { get; set; }
        public int? ParodiesCount { get; set; }
    }

    public sealed class AnimeRequest : AniDbUdpRequest
    {
        [Flags]
        public enum AMask : ulong
        {
            AnimeId = 1ul << 55,
            DateFlags = 1ul << 54,
            Year = 1ul << 53,
            Type = 1ul << 52,
            RelatedAnimeIds = 1ul << 51,
            RelatedAnimeTypes = 1ul << 50,
            // Unused = 1ul << 49,
            // Unused = 1ul << 48,

            TitleRomaji = 1ul << 47,
            TitleKanji = 1ul << 46,
            TitleEnglish = 1ul << 45,
            TitlesOther = 1ul << 44,
            TitlesShort = 1ul << 43,
            TitlesSynonym = 1ul << 42,
            // Unused = 1ul << 41,
            // Unused = 1ul << 40,

            TotalEpisodes = 1ul << 39,
            HighestEpisodeNumber = 1ul << 38,
            SpecialEpisodeCount = 1ul << 37,
            AirDate = 1ul << 36,
            EndDate = 1ul << 35,
            Url = 1ul << 34,
            PicName = 1ul << 33,
            // Unused = 1ul << 32,

            Rating = 1ul << 31,
            VoteCount = 1 << 30,
            TempRating = 1 << 29,
            TempVoteCount = 1 << 28,
            AvgReviewRating = 1 << 27,
            ReviewCount = 1 << 26,
            Awards = 1 << 25,
            IsRestricted = 1 << 24,

            // Unused = 1 << 23,
            AnnId = 1 << 22,
            AllCinemaId = 1 << 21,
            AnimeNfoId = 1 << 20,
            TagNames = 1 << 19,
            TagIds = 1 << 18,
            TagWeights = 1 << 17,
            DateRecordUpdated = 1 << 16,

            CharacterIds = 1 << 15,
            // Unused = 1 << 14,
            // Unused = 1 << 13,
            // Unused = 1 << 12,
            // Unused = 1 << 11,
            // Unused = 1 << 10,
            // Unused = 1 << 9,
            // Unused = 1 << 8,

            SpecialsCount = 1 << 7,
            CreditsCount = 1 << 6,
            OthersCount = 1 << 5,
            TrailersCount = 1 << 4,
            ParodiesCount = 1 << 3
            // Unused = 1 << 2,
            // Unused = 1 << 1,
            // Unused = 1 << 0,
        }

        public AnimeResult? AnimeResult;

        private readonly AMask _aMask;

        private AnimeRequest(IServiceProvider provider) : base(provider.GetRequiredService<ILogger<AnimeRequest>>(),
            provider.GetRequiredService<AniDbUdp>())
        {
        }

        public AnimeRequest(IServiceProvider provider, int animeId, AMask aMask) : this(provider)
        {
            _aMask = aMask;
            Params.Add(("aid", animeId.ToString()));
            Params.Add(("amask", ((ulong)aMask).ToString("X14")));
        }

        public override string Command { get; } = "ANIME";
        public override List<(string name, string value)> Params { get; } = new();

        public override async Task Process()
        {
            await SendRequest();
            switch (ResponseCode)
            {
                case AniDbResponseCode.Anime:
                    break;
                case AniDbResponseCode.NoSuchAnime:
                    break;
            }
        }

        private void GetAnimeResult()
        {
            if (ResponseText is null)
                return;
            var dataArr = ResponseText.Split('|');
            var dataIdx = 0;
            AnimeResult = new AnimeResult();
            foreach (var value in Enum.GetValues<AMask>().OrderByDescending(v => v))
                if (_aMask.HasFlag(value))
                {
                    string data = dataArr[dataIdx++];
                    if (string.IsNullOrWhiteSpace(data))
                        continue;

                    // TODO: Test switch cases
                    switch (value)
                    {
                        case AMask.AnimeId:
                            AnimeResult.AnimeId = int.Parse(data);
                            break;
                        case AMask.DateFlags:
                            AnimeResult.DateFlags = Enum.Parse<DateFlags>(data);
                            break;
                        case AMask.Year:
                            AnimeResult.Year = int.Parse(data);
                            break;
                        case AMask.Type:
                            AnimeResult.Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                            break;
                        case AMask.RelatedAnimeIds:
                            AnimeResult.RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                            break;
                        case AMask.RelatedAnimeTypes:
                            AnimeResult.RelatedAnimeTypes =
                                data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x.Replace(" ", string.Empty), true)).ToList();
                            break;
                        case AMask.TitleRomaji:
                            AnimeResult.TitleRomaji = data;
                            break;
                        case AMask.TitleKanji:
                            AnimeResult.TitleKanji = data;
                            break;
                        case AMask.TitleEnglish:
                            AnimeResult.TitleEnglish = data;
                            break;
                        case AMask.TitlesOther:
                            AnimeResult.TitlesOther = data.Split('\'').ToList();
                            break;
                        case AMask.TitlesShort:
                            AnimeResult.TitlesShort = data.Split('\'').ToList();
                            break;
                        case AMask.TitlesSynonym:
                            AnimeResult.TitlesSynonym = data.Split('\'').ToList();
                            break;
                        case AMask.TotalEpisodes:
                            AnimeResult.TotalEpisodes = int.Parse(data);
                            break;
                        case AMask.HighestEpisodeNumber:
                            AnimeResult.HighestEpisodeNumber = int.Parse(data);
                            break;
                        case AMask.SpecialEpisodeCount:
                            AnimeResult.SpecialEpisodeCount = int.Parse(data);
                            break;
                        case AMask.AirDate:
                            AnimeResult.AirDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                            break;
                        case AMask.EndDate:
                            AnimeResult.EndDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                            break;
                        case AMask.Url:
                            AnimeResult.Url = data;
                            break;
                        case AMask.PicName:
                            AnimeResult.PicName = data;
                            break;
                        case AMask.Rating:
                            AnimeResult.Rating = int.Parse(data);
                            break;
                        case AMask.VoteCount:
                            AnimeResult.VoteCount = int.Parse(data);
                            break;
                        case AMask.TempRating:
                            AnimeResult.TempRating = int.Parse(data);
                            break;
                        case AMask.TempVoteCount:
                            AnimeResult.TempVoteCount = int.Parse(data);
                            break;
                        case AMask.AvgReviewRating:
                            AnimeResult.AvgReviewRating = int.Parse(data);
                            break;
                        case AMask.ReviewCount:
                            AnimeResult.ReviewCount = int.Parse(data);
                            break;
                        case AMask.Awards:
                            AnimeResult.Awards = data.Split('\'').ToList();
                            break;
                        case AMask.IsRestricted:
                            AnimeResult.IsRestricted = int.Parse(data) != 0;
                            break;
                        case AMask.AnnId:
                            AnimeResult.AnnId = int.Parse(data);
                            break;
                        case AMask.AllCinemaId:
                            AnimeResult.AllCinemaId = int.Parse(data);
                            break;
                        case AMask.AnimeNfoId:
                            AnimeResult.AnimeNfoId = data;
                            break;
                        case AMask.TagNames:
                            AnimeResult.TagNames = data.Split('\'').ToList();
                            break;
                        case AMask.TagIds:
                            AnimeResult.TagIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                            break;
                        case AMask.TagWeights:
                            AnimeResult.TagWeights = data.Split('\'').Select(x => int.Parse(x)).ToList();
                            break;
                        case AMask.DateRecordUpdated:
                            AnimeResult.DateRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                            break;
                        case AMask.CharacterIds:
                            AnimeResult.CharacterIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                            break;
                        case AMask.SpecialsCount:
                            AnimeResult.SpecialsCount = int.Parse(data);
                            break;
                        case AMask.CreditsCount:
                            AnimeResult.CreditsCount = int.Parse(data);
                            break;
                        case AMask.OthersCount:
                            AnimeResult.OthersCount = int.Parse(data);
                            break;
                        case AMask.TrailersCount:
                            AnimeResult.TrailersCount = int.Parse(data);
                            break;
                        case AMask.ParodiesCount:
                            AnimeResult.ParodiesCount = int.Parse(data);
                            break;
                    }
                }
        }
    }
}
