using System.ComponentModel.DataAnnotations.Schema;
using ShizouCommon.Enums;

namespace ShizouData.Models;

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
    public DateTimeOffset? Updated { get; set; }
    public DateTimeOffset? AniDbUpdated { get; set; }

    public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;
}
