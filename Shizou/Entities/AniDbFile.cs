using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Dtos;

namespace Shizou.Entities
{
    [Index(nameof(Ed2K), IsUnique = true)]
    public class AniDbFile : Entity
    {
        public string Ed2K { get; set; } = null!;
        public string? Crc { get; set; }
        public string? Md5 { get; set; }
        public string? Sha1 { get; set; }
        public long FileSize { get; set; }
        public TimeSpan? Duration { get; set; }
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


        public override AniDbFileDto ToDto()
        {
            return new()
            {
                Audio = Audio.Select(a => a.ToDto()).ToList(),
                Censored = Censored,
                Chaptered = Chaptered,
                Crc = Crc,
                Deprecated = Deprecated,
                Duration = Duration,
                Id = Id,
                Md5 = Md5,
                Sha1 = Sha1,
                Source = Source,
                Subtitles = Subtitles.Select(s => s.ToDto()).ToList(),
                Updated = Updated,
                Video = Video,
                Ed2K = Ed2K,
                FileSize = FileSize,
                FileVersion = FileVersion,
                FileName = FileName,
                AniDbGroupId = AniDbGroupId,
                LocalFileId = LocalFile?.Id,
                MyListEntry = MyListEntry
            };
        }
    }
}
