using Shizou.Entities;

namespace Shizou.Dtos
{
    public class AniDbSubtitleDto : EntityDto
    {
        public int Number { get; set; }
        public string Format { get; set; } = null!;
        public string Language { get; set; } = null!;

        public int AniDbFileId { get; set; }


        public override AniDbSubtitle ToEntity()
        {
            return new()
            {
                Format = Format,
                Id = Id,
                Language = Language,
                Number = Number,
                AniDbFileId = AniDbFileId
            };
        }
    }
}
