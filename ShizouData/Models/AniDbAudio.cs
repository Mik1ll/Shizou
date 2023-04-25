using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShizouData.Models;

[Owned]
public class AniDbAudio : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Language { get; set; }
    public required string Codec { get; set; }
    public required int Bitrate { get; set; }

    public required int AniDbFileId { get; set; }
    public AniDbFile AniDbFile { get; set; } = null!;
}
