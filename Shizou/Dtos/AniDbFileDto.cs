using System;
using System.Collections.Generic;
using Shizou.Models;

namespace Shizou.Dtos;

public class AniDbFileDto : IEntityDto
{
    public int Id { get; set; }
    public string Ed2K { get; set; } = null!;
    public string? Crc { get; set; }
    public string? Md5 { get; set; }
    public string? Sha1 { get; set; }
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset? Updated { get; set; }
    public int FileVersion { get; set; }
    public string FileName { get; set; } = null!;
    public bool? Censored { get; set; }
    public bool Deprecated { get; set; }
    public bool Chaptered { get; set; }
    public bool Watched { get; set; }
    public DateTimeOffset? WatchedUpdated { get; set; }

    public AniDbMyListEntry? MyListEntry { get; set; }

    public int? AniDbGroupId { get; set; }
    public AniDbVideo? Video { get; set; }
    public List<AniDbAudioDto> Audio { get; set; } = null!;
    public List<AniDbSubtitleDto> Subtitles { get; set; } = null!;
}
