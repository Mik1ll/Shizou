using System;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Entities
{
    [Index(nameof(Hash), IsUnique = true)]
    public class AniDbFile
    {
        public int Id { get; set; }

        public string Hash { get; set; } = null!;
        public string? Source { get; set; } = null!;
        public string? AudioCodec { get; set; } = null!;
        public string? VideoCodec { get; set; } = null!;
        public DateTime? ReleaseDate { get; set; }
        public DateTime Upadate { get; set; }
        public bool WatchedStatus { get; set; }
        public DateTime? WatchedDate { get; set; }
        public string Crc { get; set; } = null!;
        public string Md5 { get; set; } = null!;
        public string Sha1 { get; set; } = null!;
        public string FIleName { get; set; } = null!;
        public long FileSize { get; set; }
        public int FileVersion { get; set; }
        public bool Censored { get; set; }
        public bool Deprecated { get; set; }

        public Collection<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
        public AniDbGroup? AniDbGroup { get; set; } = null!;
    }
}
