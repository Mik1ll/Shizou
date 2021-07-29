using System;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Dtos
{
    public class AniDbEpisodeDto : EntityDto
    {
        public string TitleEnglish { get; set; } = null!;
        public string? TitleRomaji { get; set; }
        public string? TitleKanji { get; set; }
        public int Number { get; set; }
        public EpisodeType EpisodeType { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Description { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? Updated { get; set; }

        public int AniDbAnimeId { get; set; }


        public override AniDbEpisode ToEntity()
        {
            return new()
            {
                TitleEnglish = TitleEnglish,
                TitleRomaji = TitleRomaji,
                TitleKanji = TitleKanji,
                Description = Description,
                Duration = Duration,
                Id = Id,
                Number = Number,
                Updated = Updated,
                AirDate = AirDate,
                EpisodeType = EpisodeType,
                AniDbAnimeId = AniDbAnimeId
            };
        }
    }
}
