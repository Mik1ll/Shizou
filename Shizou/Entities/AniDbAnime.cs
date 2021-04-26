using System;
using System.Collections.ObjectModel;
using Shizou.Enums;

namespace Shizou.Entities
{
    public class AniDbAnime
    {
        public int Id { get; set; }

        public int EpisodeCount { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? EndDate { get; set; }
        public AnimeType AnimeType { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public DateTime Updated { get; set; }
        public bool Restricted { get; set; }
        public string? ImagePath { get; set; } = null!;

        public Collection<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
    }
}
