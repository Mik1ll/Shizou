using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Dtos;
using Shizou.Enums;

namespace Shizou.Entities
{
    public class AniDbAnime : Entity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public override int Id { get; set; }
        public string Title { get; set; } = null!;
        public AnimeType AnimeType { get; set; }
        public int EpisodeCount { get; set; }
        public int HighestEpisode { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
        public bool Restricted { get; set; }
        public string? ImagePath { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime AniDbUpdated { get; set; }

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
                HighestEpisode = HighestEpisode,
                ImagePath = ImagePath,
                AniDbUpdated = AniDbUpdated
            };
        }
    }
}
