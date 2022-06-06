namespace Shizou.Dtos
{
    public class ImportFolderDto : EntityDto
    {
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public bool ScanOnImport { get; set; }
    }
}
