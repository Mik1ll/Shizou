using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

public class AniDbCharacter
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Name { get; set; }
    public required CharacterType Type { get; set; }
    public required string? ImageFilename { get; set; }

    [JsonIgnore]
    public List<AniDbCredit> AniDbCredits { get; set; } = default!;
}
