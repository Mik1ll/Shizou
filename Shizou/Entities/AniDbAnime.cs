using System;
using System.Collections.Generic;
using Shizou.Dtos;
using Shizou.Enums;

namespace Shizou.Entities
{
    public class AniDbAnime : Entity
    {
        public string Title { get; set; } = null!;
        public AnimeType AnimeType { get; set; }
        public int EpisodeCount { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
        public bool Restricted { get; set; }
        public string? ImagePath { get; set; }
        public DateTime? Updated { get; set; }

        public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;


        public override AniDbAnimeDto ToDto()
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
                ImagePath = ImagePath
            };
        }
    }
}
