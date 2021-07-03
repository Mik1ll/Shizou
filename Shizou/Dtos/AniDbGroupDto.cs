using Shizou.Entities;

namespace Shizou.Dtos
{
    public class AniDbGroupDto : EntityDto
    {
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? Url { get; set; }

        public override AniDbGroup ToEntity()
        {
            return new()
            {
                Id = Id,
                Name = Name,
                Url = Url,
                ShortName = ShortName
            };
        }
    }
}
