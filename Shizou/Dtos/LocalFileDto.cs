using Shizou.Entities;

namespace Shizou.Dtos
{
    public class LocalFileDto : EntityDto
    {
        public string Ed2K { get; set; } = null!;
        public string Crc { get; set; } = null!;
        public long FileSize { get; set; }
        public string Signature { get; set; } = null!;
        public bool Ignored { get; set; }
        public string PathTail { get; set; } = null!;

        public int ImportFolderId { get; set; }


        public override LocalFile ToEntity()
        {
            return new()
            {
                Id = Id,
                Crc = Crc,
                Ignored = Ignored,
                Signature = Signature,
                Ed2K = Ed2K,
                FileSize = FileSize,
                PathTail = PathTail,
                ImportFolderId = ImportFolderId
            };
        }
    }
}
