using Microsoft.EntityFrameworkCore;
using Shizou.Dtos;

namespace Shizou.Entities
{
    [Owned]
    public class AniDbVideo : Entity
    {
        public string Codec { get; set; } = null!;
        public int BitRate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ColorDepth { get; set; }

        public override AniDbVideoDto ToDto()
        {
            return new()
            {
                Codec = Codec,
                Height = Height,
                Id = Id,
                Width = Width,
                BitRate = BitRate,
                ColorDepth = ColorDepth
            };
        }
    }
}
