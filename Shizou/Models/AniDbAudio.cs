using Microsoft.EntityFrameworkCore;

namespace Shizou.Models;

[Owned]
public class AniDbAudio : IEntity
{
    public int Id { get; set; }
    public string Language { get; set; } = null!;
    public string Codec { get; set; } = null!;
    public int Bitrate { get; set; }

    public int AniDbFileId { get; set; }
    public AniDbFile AniDbFile { get; set; } = null!;
}