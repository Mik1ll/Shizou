using System;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Dtos
{
    public class AniDbAnimeDto : EntityDto
    {
        public int EpisodeCount { get; set; }
        public int HighestEpisode { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? EndDate { get; set; }
        public AnimeType AnimeType { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public bool Restricted { get; set; }
        public string? ImagePath { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime RecordUpdated { get; set; }

        public override AniDbAnime ToEntity()
        {
            return new()
            {
                Id = Id,
                Description = Description,
                Restricted = Restricted,
                Title = Title,
                Updated = Updated,
                AirDate = AirDate,
                AnimeType = AnimeType,
                EndDate = EndDate,
                EpisodeCount = EpisodeCount,
                HighestEpisode = HighestEpisode,
                ImagePath = ImagePath,
                RecordUpdated = RecordUpdated
            };
        }
    }
}
