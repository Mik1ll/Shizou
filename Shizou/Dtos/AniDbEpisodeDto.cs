using System;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Dtos
{
    public class AniDbEpisodeDto : EntityDto
    {
        public string Title { get; set; } = null!;
        public int? Duration { get; set; }
        public int Number { get; set; }
        public EpisodeType EpisodeType { get; set; }
        public string? Description { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? Updated { get; set; }

        public int AniDbAnimeId { get; set; }


        public override AniDbEpisode ToEntity()
        {
            return new()
            {
                Title = Title,
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
