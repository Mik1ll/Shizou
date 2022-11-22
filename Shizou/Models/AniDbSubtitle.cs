using Microsoft.EntityFrameworkCore;

namespace Shizou.Models
{
    [Owned]
    public sealed class AniDbSubtitle : IEntity
    {
        public int Id { get; set; }
        public string Language { get; set; } = null!;

        public int AniDbFileId { get; set; }
        public AniDbFile AniDbFile { get; set; } = null!;
    }
}
