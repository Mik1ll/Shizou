using System;
using Shizou.Enums;

namespace Shizou.Dtos;

public class AniDbAnimeDto : IEntityDto
{
    public int Id { get; set; }
    public int EpisodeCount { get; set; }
    public int HighestEpisode { get; set; }
    public string? AirDate { get; set; }
    public string? EndDate { get; set; }
    public AnimeType AnimeType { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public bool Restricted { get; set; }
    public string? ImagePath { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime AniDbUpdated { get; set; }
}
