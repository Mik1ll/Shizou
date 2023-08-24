using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(Ed2k), IsUnique = true)]
public class AniDbFile : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public required string Ed2k { get; set; }

    public required string? Crc { get; set; }
    public required string? Md5 { get; set; }
    public required string? Sha1 { get; set; }
    public required long FileSize { get; set; }
    public required int? DurationSeconds { get; set; }
    public required string? Source { get; set; }
    public required DateTime Updated { get; set; }
    public required int FileVersion { get; set; }
    public required string FileName { get; set; }
    public required bool? Censored { get; set; }
    public required bool Deprecated { get; set; }
    public required bool Chaptered { get; set; }

    public int? AniDbGroupId { get; set; }

    [JsonIgnore]
    public AniDbGroup? AniDbGroup { get; set; }

    public AniDbVideo? Video { get; set; }
    public List<AniDbAudio> Audio { get; set; } = null!;
    public List<AniDbSubtitle> Subtitles { get; set; } = null!;
}