﻿using System.Collections.Generic;
using Shizou.Dtos;

namespace Shizou.Entities
{
    public class AniDbGroup : Entity
    {
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
