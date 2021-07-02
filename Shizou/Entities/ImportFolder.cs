using System.Collections.Generic;
using System.Text.Json.Serialization;
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
        [JsonIgnore] public virtual ImportFolder? Destination { get; set; }
        [JsonIgnore] public virtual List<LocalFile> LocalFiles { get; set; } = null!;
    }
}
