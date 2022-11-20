using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Models
{
    [Index(nameof(Ed2K), IsUnique = true)]
    public sealed class AniDbFile : IEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Ed2K { get; set; } = null!;
        public string? Crc { get; set; }
        public string? Md5 { get; set; }
        public string? Sha1 { get; set; }
        public long FileSize { get; set; }
        public int? Duration { get; set; }
        public string? Source { get; set; }
        public DateTime? Updated { get; set; }
        public int FileVersion { get; set; }
        public string FileName { get; set; } = null!;
        public bool? Censored { get; set; }
        public bool Deprecated { get; set; }
        public bool Chaptered { get; set; }

        public AniDbMyListEntry? MyListEntry { get; set; }

        public int? AniDbGroupId { get; set; }
        public AniDbGroup? AniDbGroup { get; set; }
        public AniDbVideo? Video { get; set; }
        public List<AniDbAudio> Audio { get; set; } = null!;
        public List<AniDbSubtitle> Subtitles { get; set; } = null!;
        public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;

        public LocalFile? LocalFile { get; set; }
    }
}
