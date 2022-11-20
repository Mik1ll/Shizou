using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Models
{
    public sealed class AniDbGroup : IEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? Url { get; set; }

        public List<AniDbFile> AniDbFiles { get; set; } = null!;
    }
}
