using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(MalAnimeId), nameof(AniDbAnimeId))]
public class MalAniDbXref
{
    public required int MalAnimeId { get; set; }
    public MalAnime MalAnime { get; set; } = default!;
    public required int AniDbAnimeId { get; set; }
    public AniDbAnime AniDbAnime { get; set; } = default!;
}
