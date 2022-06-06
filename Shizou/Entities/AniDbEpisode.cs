using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Enums;

namespace Shizou.Entities
{
    public class AniDbEpisode : Entity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public override int Id { get; set; }
        public string TitleEnglish { get; set; } = null!;
        public string? TitleRomaji { get; set; }
        public string? TitleKanji { get; set; }
        public int Number { get; set; }
        public EpisodeType EpisodeType { get; set; }
        public TimeSpan? Duration { get; set; }
        public DateTime? AirDate { get; set; }
        public DateTime? Updated { get; set; }
        

        public int AniDbAnimeId { get; set; }
        public AniDbAnime AniDbAnime { get; set; } = null!;
        public List<AniDbFile> AniDbFiles { get; set; } = null!;

        [ForeignKey(nameof(LocalFile.ManualLinkEpisodeId))]
        public List<LocalFile> ManualLinkLocalFiles { get; set; } = null!;
    }
}
