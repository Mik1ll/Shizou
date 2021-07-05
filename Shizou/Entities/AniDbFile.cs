using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Dtos;
using Shizou.Enums;

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
        public int? Duration { get; set; }
        public string? Source { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public DateTime? Updated { get; set; }
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

        public int? AniDbGroupId { get; set; }
        public AniDbGroup? AniDbGroup { get; set; }
        public AniDbVideo? Video { get; set; }
        public List<AniDbAudio> Audio { get; set; } = null!;
        public List<AniDbSubtitle> Subtitles { get; set; } = null!;
        public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;


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
                Video = Video?.ToDto(),
                Watched = Watched,
                Ed2K = Ed2K,
                FileName = FileName,
                FileSize = FileSize,
                FileVersion = FileVersion,
                ReleaseDate = ReleaseDate,
                WatchedDate = WatchedDate,
                MyListId = MyListId,
                MyListState = MyListState,
                AniDbGroupId = AniDbGroupId,
                MyListFileState = MyListFileState
            };
        }
    }

    public static class AniDbFilesExtensions
    {
        public static AniDbFile? GetByEd2K(this IQueryable<AniDbFile> query, string ed2K)
        {
            return query.FirstOrDefault(e => e.Ed2K == ed2K);
        }

        public static AniDbFile? GetByLocalFile(this IQueryable<AniDbFile> query, LocalFile localFile)
        {
            return query.GetByEd2K(localFile.Ed2K);
        }
    }
}
