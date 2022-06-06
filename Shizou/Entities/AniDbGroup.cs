using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Entities
{
    public class AniDbGroup : Entity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public override int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? Url { get; set; }

        public List<AniDbFile> AniDbFiles { get; set; } = null!;
    }
}
