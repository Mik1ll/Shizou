using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(LocalFileId), nameof(Filename))]
[Index(nameof(Hash))]
public class LocalFileAttachment
{
    public required int LocalFileId { get; set; }

    [JsonIgnore]
    public LocalFile LocalFile { get; set; } = null!;

    public required string Filename { get; set; }
    public required string Hash { get; set; }
}
