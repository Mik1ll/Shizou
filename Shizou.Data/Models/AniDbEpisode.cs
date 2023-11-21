using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

public class AniDbEpisode
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string TitleEnglish { get; set; }
    public required string? TitleTranscription { get; set; }
    public required string? TitleOriginal { get; set; }
    public required int Number { get; set; }
    public required EpisodeType EpisodeType { get; set; }
    public required int? DurationMinutes { get; set; }
    public required DateTime? AirDate { get; set; }

    public required string? Summary { get; set; }

    public required DateTime Updated { get; set; }

    public required int AniDbAnimeId { get; set; }

    [JsonIgnore]
    public AniDbAnime AniDbAnime { get; set; } = null!;

    [JsonIgnore]
    public List<LocalFile> ManualLinkLocalFiles { get; set; } = null!;

    [JsonIgnore]
    public List<AniDbEpisodeFileXref> AniDbEpisodeFileXrefs { get; set; } = default!;

    [JsonIgnore]
    public List<AniDbFile> AniDbFiles { get; set; } = default!;

    [JsonIgnore]
    public EpisodeWatchedState EpisodeWatchedState { get; set; } = default!;
}
