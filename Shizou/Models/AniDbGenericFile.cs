using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Models;

public class AniDbGenericFile : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int AniDbEpisodeId { get; set; }
    public AniDbEpisode AniDbEpisode { get; set; } = null!;

    public int? MyListEntryId { get; set; }
    public AniDbMyListEntry? MyListEntry { get; set; }
}
