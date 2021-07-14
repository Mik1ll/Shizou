using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Dtos;
using Shizou.Enums;

namespace Shizou.Entities
{
    public class AniDbEpisode : Entity
    {
        public int? Duration { get; set; }
        public int Number { get; set; }
        public EpisodeType EpisodeType { get; set; }
        public string? Description { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? Updated { get; set; }

        public int AniDbAnimeId { get; set; }
        public AniDbAnime AniDbAnime { get; set; } = null!;
        public List<AniDbFile> AniDbFiles { get; set; } = null!;

        [ForeignKey(nameof(LocalFile.ManualLinkEpisodeId))]
        public List<LocalFile> ManualLinkLocalFiles { get; set; } = null!;

        public override AniDbEpisodeDto ToDto()
        {
            return new()
            {
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
