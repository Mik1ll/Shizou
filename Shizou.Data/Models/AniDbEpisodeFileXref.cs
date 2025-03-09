using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(AniDbEpisodeId), nameof(AniDbFileId))]
public class AniDbEpisodeFileXref
{
    public required int AniDbEpisodeId { get; set; }

    [JsonIgnore]
    public AniDbEpisode AniDbEpisode { get; set; } = default!;

    public required int AniDbFileId { get; set; }

    [JsonIgnore]
    public AniDbFile AniDbFile { get; set; } = default!;
}
