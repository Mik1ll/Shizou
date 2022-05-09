using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Shizou.Dtos;

namespace Shizou.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(Path), IsUnique = true)]
    public class ImportFolder : Entity
    {
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;

        public bool ScanOnImport { get; set; }

        public List<LocalFile> LocalFiles { get; set; } = null!;


        public override ImportFolderDto ToDto()
        {
            return new()
            {
                Id = Id,
                Name = Name,
                Path = Path,
                ScanOnImport = ScanOnImport
            };
        }
    }
}
