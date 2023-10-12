using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(MyListId))]
public class EpisodeWatchedState : IWatchedState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int AniDbEpisodeId { get; set; }

    public AniDbEpisode AniDbEpisode { get; set; } = default!;
    public int? AniDbFileId { get; set; }

    public required bool Watched { get; set; }
    public required DateTime? WatchedUpdated { get; set; }
    public int? MyListId { get; set; }
}
