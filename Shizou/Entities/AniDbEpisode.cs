﻿using System;
using System.Collections.Generic;
using Shizou.Enums;

namespace Shizou.Entities
{
    public class AniDbEpisode : Entity
    {
        public TimeSpan? Length { get; set; }
        public int Number { get; set; }
        public EpisodeType EpisodeType { get; set; }
        public string? Description { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime Updated { get; set; }

        public int AniDbAnimeId { get; set; }
        public virtual AniDbAnime AniDbAnime { get; set; } = null!;
        public virtual List<AniDbFile> AniDbFiles { get; set; } = null!;
    }
}
