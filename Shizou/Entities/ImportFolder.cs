using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Dtos;

namespace Shizou.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(Path), IsUnique = true)]
    public class ImportFolder : Entity
    {
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;

        public int? DestinationId { get; set; }
        public ImportFolder? Destination { get; set; }
        public List<LocalFile> LocalFiles { get; set; } = null!;


        public override ImportFolderDto ToDto()
        {
            return new()
            {
                Id = Id,
                Name = Name,
                Path = Path,
                DestinationId = DestinationId
            };
        }
    }

    public static class ImportFolderExtensions
    {
        public static ImportFolder? GetByPath(this IQueryable<ImportFolder> query, string path)
        {
            return query.OrderByDescending(i => i.Path.Length).FirstOrDefault(i => path.StartsWith(i.Path));
        }
    }
}
