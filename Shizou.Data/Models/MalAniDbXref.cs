using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(MalAnimesId), nameof(AniDbAnimesId))]
public class MalAniDbXref
{
    public required int MalAnimesId { get; set; }
    public MalAnime MalAnime { get; set; } = default!;
    public required int AniDbAnimesId { get; set; }
    public AniDbAnime AniDbAnime { get; set; } = default!;
}
