﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.AniDbApi.Requests;
using Shizou.Enums;

namespace Shizou.Models
{
    [Index(nameof(Ed2K), IsUnique = true)]
    public sealed class AniDbFile : IEntity
    {
        public AniDbFile()
        {
        }

        public AniDbFile(FileRequest.AniDbFileResult result)
        {
            Id = result.FileId;
            Ed2K = result.Ed2K!;
            Md5 = result.Md5;
            Crc = result.Crc32;
            Sha1 = result.Sha1;
            Censored = result.State!.Value.IsCensored();
            Chaptered = result.State!.Value.HasFlag(FileState.Chaptered);
            Deprecated = result.IsDeprecated!.Value;
            FileSize = result.Size!.Value;
            Duration = result.LengthInSeconds;
            Source = result.Source;
            FileVersion = result.State!.Value.FileVersion();
            Updated = DateTime.UtcNow;
            AniDbGroupId = result.GroupId;
            MyListEntry = result.MyListId is null
                ? null
                : new AniDbMyListEntry
                {
                    Id = result.MyListId!.Value,
                    Watched = result.MyListViewed!.Value,
                    WatchedDate = result.MyListViewDate,
                    MyListState = result.MyListState!.Value,
                    MyListFileState = result.MyListFileState!.Value
                };
            Audio = result.AudioCodecs!.Zip(result.AudioBitRates!, (codec, bitrate) => (codec, bitrate))
                .Zip(result.DubLanguages!, (tup, lang) => (tup.codec, tup.bitrate, lang)).Select((tuple, i) =>
                    new AniDbAudio { Bitrate = tuple.bitrate, Codec = tuple.codec, Language = tuple.lang, Id = i + 1 }).ToList();
            Video = result.VideoCodec is null
                ? null
                : new AniDbVideo
                {
                    Codec = result.VideoCodec,
                    BitRate = result.VideoBitRate!.Value,
                    ColorDepth = result.VideoColorDepth ?? 8,
                    Height = int.Parse(result.VideoResolution!.Split('x')[1]),
                    Width = int.Parse(result.VideoResolution!.Split('x')[0])
                };
            Subtitles = result.SubLangugages!.Select((s, i) => new AniDbSubtitle { Language = s, Id = i + 1 }).ToList();
            FileName = result.AniDbFileName!;
        }

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
    }
}
