using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(MalId), nameof(AniDbId))]
public class MalAniDbXref
{
    public required int MalId { get; set; }
    public required int AniDbId { get; set; }
}