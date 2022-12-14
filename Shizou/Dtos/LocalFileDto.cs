using System;

namespace Shizou.Dtos;

public class LocalFileDto : IEntityDto
{
    public int Id { get; set; }
    public string Ed2K { get; set; } = null!;
    public string Crc { get; set; } = null!;
    public long FileSize { get; set; }
    public string Signature { get; set; } = null!;
    public bool Ignored { get; set; }
    public string PathTail { get; set; } = null!;
    public DateTime? Updated { get; set; }

    public int ImportFolderId { get; set; }
}
