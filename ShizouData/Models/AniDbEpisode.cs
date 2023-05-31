using System.ComponentModel.DataAnnotations.Schema;
using ShizouCommon.Enums;

namespace ShizouData.Models;

public class AniDbEpisode : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string TitleEnglish { get; set; }
    public required string? TitleRomaji { get; set; }
    public required string? TitleKanji { get; set; }
    public required int Number { get; set; }
    public required EpisodeType EpisodeType { get; set; }
    public required int? DurationMinutes { get; set; }
    public required DateTimeOffset? AirDate { get; set; }
    public bool Watched { get; set; }
    public DateTimeOffset? WatchedUpdated { get; set; }

    public DateTimeOffset? Updated { get; set; }

    public required int AniDbAnimeId { get; set; }
    public AniDbAnime AniDbAnime { get; set; } = null!;

    public List<LocalFile> ManualLinkLocalFiles { get; set; } = null!;
}
