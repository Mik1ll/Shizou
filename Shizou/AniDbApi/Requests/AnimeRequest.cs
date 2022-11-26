using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Results;
using Shizou.CommandProcessors;

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
