using Shizou.Entities;

namespace Shizou.Dtos
{
    public class AniDbSubtitleDto : EntityDto
    {
        public int Number { get; set; }
        public string Language { get; set; } = null!;


        public override AniDbSubtitle ToEntity()
        {
            return new()
            {
                Id = Id,
                Language = Language,
                Number = Number
            };
        }
    }
}
