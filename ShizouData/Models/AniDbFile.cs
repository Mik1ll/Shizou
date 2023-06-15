using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShizouData.Models;

[Index(nameof(Ed2K), IsUnique = true)]
public class AniDbFile : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Ed2K { get; set; }
    public required string? Crc { get; set; }
    public required string? Md5 { get; set; }
    public required string? Sha1 { get; set; }
    public required long FileSize { get; set; }
    public required int? DurationSeconds { get; set; }
    public required string? Source { get; set; }
    public DateTime? Updated { get; set; }
    public required int FileVersion { get; set; }
    public required string FileName { get; set; }
    public required bool? Censored { get; set; }
    public required bool Deprecated { get; set; }
    public required bool Chaptered { get; set; }
    public bool Watched { get; set; }
    public DateTime? WatchedUpdated { get; set; }

    public int? MyListEntryId { get; set; }
    public AniDbMyListEntry? MyListEntry { get; set; }
    public int? AniDbGroupId { get; set; }
    public AniDbGroup? AniDbGroup { get; set; }
    public AniDbVideo? Video { get; set; }
    public List<AniDbAudio> Audio { get; set; } = null!;
    public List<AniDbSubtitle> Subtitles { get; set; } = null!;
}
