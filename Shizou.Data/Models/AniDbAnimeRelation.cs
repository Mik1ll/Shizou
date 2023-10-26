using Microsoft.EntityFrameworkCore;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(AnimeId), nameof(ToAnimeId), nameof(RelationType))]
public class AniDbAnimeRelation
{
    public required int AnimeId { get; set; }
    public required int ToAnimeId { get; set; }
    public required RelatedAnimeType RelationType { get; set; }
}
