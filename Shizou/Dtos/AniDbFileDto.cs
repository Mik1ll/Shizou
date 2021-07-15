using System;
using System.Collections.Generic;
using System.Linq;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Dtos
{
    public class MyListEntryDto : EntityDto
    {
        public bool Watched { get; set; }
        public DateTime? WatchedDate { get; set; }
        public MyListState MyListState { get; set; }
        public MyListFileState MyListFileState { get; set; }

        public override MyListEntry ToEntity()
        {
            return new()
            {
                Id = Id,
                Watched = Watched,
                WatchedDate = WatchedDate,
                MyListState = MyListState,
                MyListFileState = MyListFileState
            };
        }
    }

    public class AniDbFileDto : EntityDto
    {
        public string Ed2K { get; set; } = null!;
        public string? Crc { get; set; }
        public string? Md5 { get; set; }
        public string? Sha1 { get; set; }
        public long FileSize { get; set; }
        public int? Duration { get; set; }
        public string? Source { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public DateTime? Updated { get; set; }
        public int FileVersion { get; set; }
        public bool Censored { get; set; }
        public bool Deprecated { get; set; }
        public bool Chaptered { get; set; }

        public MyListEntryDto? MyListEntry { get; set; }

        public int? AniDbGroupId { get; set; }
        public AniDbVideoDto? Video { get; set; }
        public List<AniDbAudioDto> Audio { get; set; } = null!;
        public List<AniDbSubtitleDto> Subtitles { get; set; } = null!;

        public int? LocalFileId { get; set; }


        public override AniDbFile ToEntity()
        {
            return new()
            {
                Audio = Audio.Select(a => a.ToEntity()).ToList(),
                Censored = Censored,
                Chaptered = Chaptered,
                Crc = Crc,
                Deprecated = Deprecated,
                Duration = Duration,
                Id = Id,
                Md5 = Md5,
                Sha1 = Sha1,
                Source = Source,
                Subtitles = Subtitles.Select(s => s.ToEntity()).ToList(),
                Updated = Updated,
                Video = Video?.ToEntity(),
                Ed2K = Ed2K,
                FileSize = FileSize,
                FileVersion = FileVersion,
                ReleaseDate = ReleaseDate,
                AniDbGroupId = AniDbGroupId,
                MyListEntry = MyListEntry?.ToEntity()
            };
        }
    }
}
