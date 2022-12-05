using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Models;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Path), IsUnique = true)]
public sealed class ImportFolder : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;

    public bool ScanOnImport { get; set; }

    public List<LocalFile> LocalFiles { get; set; } = null!;
}