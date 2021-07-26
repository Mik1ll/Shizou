using Microsoft.EntityFrameworkCore;

namespace Shizou.Entities
{
    [Owned]
    public class AniDbVideo
    {
        public string Codec { get; set; } = null!;
        public int BitRate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ColorDepth { get; set; }
    }
}
