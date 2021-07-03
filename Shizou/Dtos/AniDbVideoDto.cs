using Shizou.Entities;

namespace Shizou.Dtos
{
    public class AniDbVideoDto : EntityDto
    {
        public string Codec { get; set; } = null!;
        public int BitRate { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ColorDepth { get; set; }

        public int AniDbFileId { get; set; }


        public override AniDbVideo ToEntity()
        {
            return new()
            {
                Codec = Codec,
                Height = Height,
                Id = Id,
                Width = Width,
                BitRate = BitRate,
                ColorDepth = ColorDepth,
                AniDbFileId = AniDbFileId
            };
        }
    }
}
