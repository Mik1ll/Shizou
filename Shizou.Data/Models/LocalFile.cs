using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(Ed2k), IsUnique = true)]
[Index(nameof(Signature), IsUnique = true)]
[Index(nameof(ImportFolderId), nameof(PathTail), IsUnique = true)]
public class LocalFile
{
    public int Id { get; set; }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public required string Ed2k { get; set; }

    public required string Crc { get; set; }
    public required long FileSize { get; set; }
    public required string Signature { get; set; }
    public required bool Ignored { get; set; }
    public required string PathTail { get; set; }
    public DateTime? Updated { get; set; }

    public int? ImportFolderId { get; set; }

    [JsonIgnore]
    public ImportFolder? ImportFolder { get; set; }

    public int? AniDbFileId { get; set; }

    [JsonIgnore]
    public AniDbFile? AniDbFile { get; set; }
}
