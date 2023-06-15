using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Path), IsUnique = true)]
public class ImportFolder : IEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }

    public required bool ScanOnImport { get; set; }

    public List<LocalFile> LocalFiles { get; set; } = null!;
}
