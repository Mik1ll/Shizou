using System.ComponentModel.DataAnnotations;

namespace ShizouContracts.Dtos;

public class ImportFolderDto : IEntityDto
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Path { get; set; } = null!;
    public bool ScanOnImport { get; set; }
}
