using ShizouCommon.Enums;

namespace ShizouContracts.Dtos;

public class AniDbEpisodeDto : IEntityDto
{
    public int Id { get; set; }
    public string TitleEnglish { get; set; } = null!;
    public string? TitleRomaji { get; set; }
    public string? TitleKanji { get; set; }
    public int Number { get; set; }
    public EpisodeType EpisodeType { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTimeOffset? AirDate { get; set; }
    public bool Watched { get; set; }
    public DateTimeOffset? WatchedUpdated { get; set; }

    public DateTimeOffset? Updated { get; set; }

    
    public int AniDbAnimeId { get; set; }
}
