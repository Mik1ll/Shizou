using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ShizouData.Models;

[Index(nameof(AniDbEpisodeId), IsUnique = true)]
public class AniDbGenericFile : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int AniDbEpisodeId { get; set; }

    public int? MyListEntryId { get; set; }
    public AniDbMyListEntry? MyListEntry { get; set; }
}
