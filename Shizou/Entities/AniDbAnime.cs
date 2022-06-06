using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
        public string? AirDate { get; set; }
        public string? EndDate { get; set; }
        public string? Description { get; set; }
        public bool Restricted { get; set; }
        public string? ImagePath { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime AniDbUpdated { get; set; }

        public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
    }
}
