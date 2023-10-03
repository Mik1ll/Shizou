using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Shizou.Data.Models;

public class AniDbGroup
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public required string? Url { get; set; }

    [JsonIgnore]
    public List<AniDbFile> AniDbFiles { get; set; } = null!;
}
