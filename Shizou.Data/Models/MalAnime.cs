using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Shizou.Data.Models;

public class MalAnime
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public required string Title { get; set; }
    public required string AnimeType { get; set; }
    public required int? EpisodeCount { get; set; }
    public MalStatus? Status { get; set; }

    [JsonIgnore]
    public List<MalAniDbXref> MalAniDbXrefs { get; set; } = default!;

    [JsonIgnore]
    public List<AniDbAnime> AniDbAnimes { get; set; } = default!;
}
