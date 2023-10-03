using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Validation;

namespace Shizou.Data.Models;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Path), IsUnique = true)]
public class ImportFolder
{
    public int Id { get; set; }

    [Required]
    [Unique]
    public required string Name { get; set; }

    [Required]
    [Unique]
    public required string Path { get; set; }

    public bool ScanOnImport { get; set; }

    [JsonIgnore]
    public List<LocalFile> LocalFiles { get; set; } = null!;
}
