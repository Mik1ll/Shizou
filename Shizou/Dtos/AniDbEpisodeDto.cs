using System;
using Shizou.Enums;

namespace Shizou.Dtos;

public class AniDbEpisodeDto : IEntityDto
{
    public int Id { get; set; }
    public string TitleEnglish { get; set; } = null!;
    public string? TitleRomaji { get; set; }
    public string? TitleKanji { get; set; }
    public int Number { get; set; }
    public EpisodeType EpisodeType { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? AirDate { get; set; }
    public DateTime? Updated { get; set; }

    public int AniDbAnimeId { get; set; }
    public int? GenericMyListEntryId { get; set; }
}
