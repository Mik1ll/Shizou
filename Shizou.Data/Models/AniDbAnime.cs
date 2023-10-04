﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

public class AniDbAnime
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
    public required string? ImageFilename { get; set; }
    public required DateTime Updated { get; set; }
    public DateTime? AniDbUpdated { get; set; }

    [JsonIgnore]
    public List<AniDbEpisode> AniDbEpisodes { get; set; } = null!;

    [JsonIgnore]
    public List<MalAniDbXref> MalAniDbXrefs { get; set; } = default!;

    [JsonIgnore]
    public List<MalAnime> MalAnimes { get; set; } = default!;
}
