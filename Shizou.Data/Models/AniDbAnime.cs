using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Shizou.Common.Enums;

namespace Shizou.Data.Models;

public class AniDbAnime : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Title { get; set; }
    public required AnimeType AnimeType { get; set; }
    public required int EpisodeCount { get; set; }
    public required string? AirDate { get; set; }
    public required string? EndDate { get; set; }
    public required string? Description { get; set; }
    public required bool Restricted { get; set; }
    public required string? ImagePath { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime? AniDbUpdated { get; set; }

    [JsonIgnore]
    public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
}
