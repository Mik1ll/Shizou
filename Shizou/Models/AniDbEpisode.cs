using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.AniDbApi.Requests;
using Shizou.Enums;

namespace Shizou.Models
{
    public sealed class AniDbEpisode : IEntity
    {
        public AniDbEpisode()
        {
        }

        public AniDbEpisode(EpisodeRequest.AniDbEpisodeResult epResult)
        {
            Id = epResult.EpisodeId;
            TitleEnglish = epResult.TitleEnglish;
            TitleRomaji = epResult.TitleRomaji;
            TitleKanji = epResult.TitleKanji;
            Number = epResult.EpisodeNumber;
            EpisodeType = epResult.Type;
            Duration = epResult.DurationMinutes is null ? null : TimeSpan.FromMinutes(epResult.DurationMinutes.Value);
            AirDate = epResult.AiredDate;
            Updated = DateTime.UtcNow;
            AniDbFiles = new List<AniDbFile>();
            AniDbAnimeId = epResult.AnimeId;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

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
