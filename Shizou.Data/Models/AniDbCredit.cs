using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(AniDbAnimeId), nameof(Id))]
public class AniDbCredit
{
    public required int AniDbAnimeId { get; set; }

    [JsonIgnore]
    public AniDbAnime AniDbAnime { get; set; } = default!;

    public required int Id { get; set; }
    public required string? Role { get; set; }

    public required int AniDbCreatorId { get; set; }

    [JsonIgnore]
    public AniDbCreator AniDbCreator { get; set; } = default!;

    public int? AniDbCharacterId { get; set; }

    [JsonIgnore]
    public AniDbCharacter? AniDbCharacter { get; set; }
}
