using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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
        public string AudioCodecsJson { get; set; } = null!;

        [NotMapped]
        public List<Codec> AudioCodecs
        {
            get => JsonSerializer.Deserialize<List<Codec>>(string.IsNullOrWhiteSpace(AudioCodecsJson) ? "[]" : AudioCodecsJson)!;
            set => AudioCodecsJson = JsonSerializer.Serialize(value);
        }

        public string VideoCodecsJson { get; set; } = null!;

        [NotMapped]
        public List<Codec> VideoCodecs
        {
            get => JsonSerializer.Deserialize<List<Codec>>(string.IsNullOrWhiteSpace(VideoCodecsJson) ? "[]" : VideoCodecsJson)!;
            set => VideoCodecsJson = JsonSerializer.Serialize(value);
        }

        public DateTime? ReleaseDate { get; set; }
        public DateTime Updated { get; set; }
        public bool WatchedStatus { get; set; }
        public DateTime? WatchedDate { get; set; }
        public string FileName { get; set; } = null!;
        public int FileVersion { get; set; }
        public bool Censored { get; set; }
        public bool Deprecated { get; set; }
        public bool Chaptered { get; set; }


        public int? AniDbGroupId { get; set; }
        public AniDbGroup? AniDbGroup { get; set; } = null!;
        public ICollection<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
    }
}
