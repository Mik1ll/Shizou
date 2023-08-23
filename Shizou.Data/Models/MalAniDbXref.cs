using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(MalId), nameof(AniDbId))]
public class MalAniDbXref
{
    public int MalId { get; set; }
    public int AniDbId { get; set; }
}