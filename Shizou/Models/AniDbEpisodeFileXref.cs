using Microsoft.EntityFrameworkCore;

namespace Shizou.Models;

[PrimaryKey(nameof(AniDbEpisodeId), nameof(AniDbFileId))]
public class AniDbEpisodeFileXref
{
    public int AniDbEpisodeId { get; set; }
    public int AniDbFileId { get; set; }
}
