using System;
using System.Collections.ObjectModel;
using Shizou.Enums;

namespace Shizou.Entities
{
    public class AniDbEpisode : Entity
    {
        public AniDbAnime AniDbAnime { get; set; } = null!;
        public TimeSpan? Length { get; set; }
        public int Number { get; set; }
        public EpisodeType EpisodeType { get; set; }
        public string? Description { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime Updated { get; set; }
        public Collection<AniDbFile> AniDbFiles { get; set; } = null!;
    }
}
