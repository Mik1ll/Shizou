using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(AniDbEpisodeId), nameof(AniDbNormalFileId))]
public class HangingEpisodeFileXref
{
    public required int AniDbEpisodeId { get; set; }
    public required int AniDbNormalFileId { get; set; }
    public AniDbNormalFile AniDbNormalFile { get; set; } = default!;
}
