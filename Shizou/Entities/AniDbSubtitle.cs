using Shizou.Dtos;

namespace Shizou.Entities
{
    public class AniDbSubtitle : Entity
    {
        public int Number { get; set; }
        public string Format { get; set; } = null!;
        public string Language { get; set; } = null!;

        public int AniDbFileId { get; set; }
        public AniDbFile AniDbFile { get; set; } = null!;

        public override AniDbSubtitleDto ToDto()
        {
            return new()
            {
                Id = Id,
                Format = Format,
                Language = Language,
                Number = Number
            };
        }
    }
}
