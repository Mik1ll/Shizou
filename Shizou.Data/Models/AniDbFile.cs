using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Shizou.Data.Models;

public abstract class AniDbFile
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }


    [JsonIgnore]
    public List<AniDbEpisode> AniDbEpisodes { get; set; } = default!;

    [JsonIgnore]
    public List<AniDbEpisodeFileXref> AniDbEpisodeFileXrefs { get; set; } = default!;

    [JsonIgnore]
    public FileWatchedState FileWatchedState { get; set; } = default!;

    [JsonIgnore]
    public List<LocalFile> LocalFiles { get; set; } = default!;
}
