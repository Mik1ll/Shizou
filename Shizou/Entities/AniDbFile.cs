using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Shizou.Enums;

namespace Shizou.Entities
{
    public sealed record Codec(string Name, int Bitrate);

    [Index(nameof(Ed2K), IsUnique = true)]
    public class AniDbFile : Entity
    {
        public string Ed2K { get; set; } = null!;
        public string? Crc { get; set; }
        public string? Md5 { get; set; }
        public string? Sha1 { get; set; }
        public long FileSize { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Source { get; set; } = null!;
        public DateTime? ReleaseDate { get; set; }
        public DateTime Updated { get; set; }
        public string FileName { get; set; } = null!;
        public int FileVersion { get; set; }
        public bool Censored { get; set; }
        public bool Deprecated { get; set; }
        public bool Chaptered { get; set; }

        public int MyListId { get; set; }
        public bool Watched { get; set; }
        public DateTime? WatchedDate { get; set; }
        public MyListState MyListState { get; set; }
        public MyListFileState MyListFileState { get; set; }

        public virtual AniDbVideo? Video { get; set; }
        public int? AniDbGroupId { get; set; }
        public virtual AniDbGroup? AniDbGroup { get; set; } = null!;
        public virtual ICollection<AniDbAudio> Audio { get; set; } = null!;
        public virtual ICollection<AniDbSubtitle> Subtitles { get; set; } = null!;
        public virtual ICollection<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
    }
}
