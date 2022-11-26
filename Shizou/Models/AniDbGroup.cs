using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.AniDbApi.Results;

namespace Shizou.Models
{
    public sealed class AniDbGroup : IEntity
    {
        public AniDbGroup()
        {
        }

        public AniDbGroup(AniDbFileResult result)
        {
            Id = result.GroupId!.Value;
            Name = result.GroupName!;
            ShortName = result.GroupNameShort!;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? Url { get; set; }

        public List<AniDbFile> AniDbFiles { get; set; } = null!;
    }
}
