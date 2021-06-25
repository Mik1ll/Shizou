using Microsoft.EntityFrameworkCore;

namespace Shizou.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(Location), IsUnique = true)]
    public class ImportFolder : Entity
    {
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public ImportFolder? Destination { get; set; }
    }
}
