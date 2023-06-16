using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Path), IsUnique = true)]
public class ImportFolder : IEntity
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string Path { get; set; }

    public bool ScanOnImport { get; set; }

    [JsonIgnore]
    public List<LocalFile> LocalFiles { get; set; } = null!;
}
