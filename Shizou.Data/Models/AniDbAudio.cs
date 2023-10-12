using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Owned]
public class AniDbAudio
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Language { get; set; }
    public required string Codec { get; set; }
    public required int Bitrate { get; set; }

    public required int AniDbFileId { get; set; }

    [JsonIgnore]
    public AniDbFile AniDbFile { get; set; } = null!;
}
