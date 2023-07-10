using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(Ed2K), IsUnique = true)]
[Index(nameof(Signature), IsUnique = true)]
[Index(nameof(ImportFolderId), nameof(PathTail), IsUnique = true)]
public class LocalFile : IEntity
{
    public int Id { get; set; }
    public required string Ed2K { get; set; }
    public required string Crc { get; set; }
    public required long FileSize { get; set; }
    public required string Signature { get; set; }
    public required bool Ignored { get; set; }
    public required string PathTail { get; set; }
    public DateTime? Updated { get; set; }

    public required int ImportFolderId { get; set; }
    [JsonIgnore] public ImportFolder ImportFolder { get; set; } = null!;

    [JsonIgnore]
    public List<AniDbEpisode> ManualLinkEpisodes { get; set; } = null!;

    [JsonIgnore]
    public List<ManualLinkXref> ManualLinkXrefs { get; set; } = null!;
}
