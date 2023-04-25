using Microsoft.EntityFrameworkCore;

namespace ShizouData.Models;

[PrimaryKey(nameof(AniDbEpisodeId), nameof(AniDbFileId))]
public class AniDbEpisodeFileXref
{
    public required int AniDbEpisodeId { get; set; }
    public required int AniDbFileId { get; set; }
}
