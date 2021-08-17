using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Dtos;

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


        public override AniDbGroupDto ToDto()
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
