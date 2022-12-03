namespace Shizou.Dtos;

public class ImportFolderDto : IEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public bool ScanOnImport { get; set; }
}
