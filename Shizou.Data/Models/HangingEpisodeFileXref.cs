using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(AniDbEpisodeId), nameof(AniDbFileId))]
public class HangingEpisodeFileXref
{
    public required int AniDbEpisodeId { get; set; }
    public required int AniDbFileId { get; set; }
    public AniDbFile AniDbFile { get; set; } = default!;
}
