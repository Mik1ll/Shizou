using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(Path), IsUnique = true)]
    public class ImportFolder : Entity
    {
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;

        public int? DestinationId { get; set; }
        public ImportFolder? Destination { get; set; }
        public List<LocalFile> LocalFiles { get; set; } = null!;
    }
}
