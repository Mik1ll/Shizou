using System;
using System.Collections.Generic;
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
        public string? Description { get; set; } = null!;
        public bool Restricted { get; set; }
        public string? ImagePath { get; set; } = null!;
        public DateTime Updated { get; set; }

        public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;

        public virtual List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
    }
}
