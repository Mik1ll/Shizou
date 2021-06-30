using System.Collections.Generic;

namespace Shizou.Entities
{
    public class AniDbGroup : Entity
    {
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? Url { get; set; }

        public virtual List<AniDbFile> AniDbFiles { get; set; } = null!;
    }
}
