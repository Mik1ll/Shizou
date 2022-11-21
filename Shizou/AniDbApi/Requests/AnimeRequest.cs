using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests
{
    public sealed class AnimeRequest : AniDbUdpRequest
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

        public sealed record AniDbAnimeResult
        {
            public AniDbAnimeResult(string responseText, AMask mask)
            {
                var dataArr = responseText.TrimEnd().Split('|');
                var dataIdx = 0;
                foreach (var value in Enum.GetValues<AMask>().OrderByDescending(v => v))
                    if (mask.HasFlag(value))
                    {
                        var data = dataArr[dataIdx++];
                        if (string.IsNullOrWhiteSpace(data))
                            continue;

                        // TODO: Test switch cases
                        switch (value)
                        {
                            case AMask.AnimeId:
                                AnimeId = int.Parse(data);
                                break;
                            case AMask.DateFlags:
                                DateInfo = Enum.Parse<DateFlags>(data);
                                break;
                            case AMask.Year:
                                Year = data;
                                break;
                            case AMask.Type:
                                Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                                break;
                            case AMask.RelatedAnimeIds:
                                RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                                break;
                            case AMask.RelatedAnimeTypes:
                                RelatedAnimeTypes =
                                    data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x)).ToList();
                                break;
                            case AMask.TitleRomaji:
                                TitleRomaji = data;
                                break;
                            case AMask.TitleKanji:
                                TitleKanji = data;
                                break;
                            case AMask.TitleEnglish:
                                TitleEnglish = data;
                                break;
                            case AMask.TitlesOther:
                                TitlesOther = data.Split('\'').ToList();
                                break;
                            case AMask.TitlesShort:
                                TitlesShort = data.Split('\'').ToList();
                                break;
                            case AMask.TitlesSynonym:
                                TitlesSynonym = data.Split('\'').ToList();
                                break;
                            case AMask.TotalEpisodes:
                                TotalEpisodes = int.Parse(data);
                                break;
                            case AMask.HighestEpisodeNumber:
                                HighestEpisodeNumber = int.Parse(data);
                                break;
                            case AMask.SpecialEpisodeCount:
                                SpecialEpisodeCount = int.Parse(data);
                                break;
                            case AMask.AirDate:
                                AirDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                                break;
                            case AMask.EndDate:
                                EndDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                                break;
                            case AMask.Url:
                                Url = data;
                                break;
                            case AMask.PicName:
                                PicName = data;
                                break;
                            case AMask.Rating:
                                Rating = int.Parse(data);
                                break;
                            case AMask.VoteCount:
                                VoteCount = int.Parse(data);
                                break;
                            case AMask.TempRating:
                                TempRating = int.Parse(data);
                                break;
                            case AMask.TempVoteCount:
                                TempVoteCount = int.Parse(data);
                                break;
                            case AMask.AvgReviewRating:
                                AvgReviewRating = int.Parse(data);
                                break;
                            case AMask.ReviewCount:
                                ReviewCount = int.Parse(data);
                                break;
                            case AMask.Awards:
                                Awards = data.Split('\'').ToList();
                                break;
                            case AMask.IsRestricted:
                                IsRestricted = int.Parse(data) != 0;
                                break;
                            case AMask.AnnId:
                                AnnId = int.Parse(data);
                                break;
                            case AMask.AllCinemaId:
                                AllCinemaId = int.Parse(data);
                                break;
                            case AMask.AnimeNfoId:
                                AnimeNfoId = data;
                                break;
                            case AMask.TagNames:
                                TagNames = data.Split(',').ToList();
                                break;
                            case AMask.TagIds:
                                TagIds = data.Split(',').Select(x => int.Parse(x)).ToList();
                                break;
                            case AMask.TagWeights:
                                TagWeights = data.Split(',').Select(x => int.Parse(x)).ToList();
                                break;
                            case AMask.DateRecordUpdated:
                                DateRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                                break;
                            case AMask.CharacterIds:
                                CharacterIds = data.Split(',').Select(x => int.Parse(x)).ToList();
                                break;
                            case AMask.SpecialsCount:
                                SpecialsCount = int.Parse(data);
                                break;
                            case AMask.CreditsCount:
                                CreditsCount = int.Parse(data);
                                break;
                            case AMask.OthersCount:
                                OthersCount = int.Parse(data);
                                break;
                            case AMask.TrailersCount:
                                TrailersCount = int.Parse(data);
                                break;
                            case AMask.ParodiesCount:
                                ParodiesCount = int.Parse(data);
                                break;
                        }
                    }
            }

            public int? AnimeId { get; }
            public DateFlags? DateInfo { get; }
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

        public const AMask DefaultAMask = AMask.DateRecordUpdated | AMask.TitleRomaji | AMask.TotalEpisodes | AMask.HighestEpisodeNumber | AMask.Type |
                                          AMask.TitleEnglish | AMask.TitleKanji | AMask.AnimeId;

        public AniDbAnimeResult? AnimeResult;

        private readonly AMask _aMask;

        private AnimeRequest(IServiceProvider provider) : base(provider.GetRequiredService<ILogger<AnimeRequest>>(),
            provider.GetRequiredService<AniDbUdp>(),
            provider.GetRequiredService<AniDbUdpProcessor>())
        {
        }

        public AnimeRequest(IServiceProvider provider, int animeId, AMask aMask) : this(provider)
        {
            _aMask = aMask;
            Params["aid"] = animeId.ToString();
            Params["amask"] = ((ulong)aMask).ToString("X14");
        }

        public override string Command { get; } = "ANIME";
        public override Dictionary<string, string> Params { get; } = new();

        public override async Task Process()
        {
            await SendRequest();
            switch (ResponseCode)
            {
                case AniDbResponseCode.Anime:
                    if (string.IsNullOrWhiteSpace(ResponseText))
                        Errored = true;
                    else
                        AnimeResult = new AniDbAnimeResult(ResponseText, _aMask);
                    break;
                case AniDbResponseCode.NoSuchAnime:
                    break;
            }
        }
    }
}
