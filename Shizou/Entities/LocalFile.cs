using System;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Entities
{
    [Index(nameof(Ed2K), IsUnique = true)]
    [Index(nameof(Signature), IsUnique = true)]
    public class LocalFile : Entity
    {
        public string Ed2K { get; set; } = null!;
        public string Crc { get; set; } = null!;
        public long FileSize { get; set; }
        public string Signature { get; set; } = null!;
        public bool Ignored { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string PathTail { get; set; } = null!;

        public ImportFolder ImportFolder { get; set; } = null!;
    }
}
