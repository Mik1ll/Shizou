using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Data.Models;

public class EpisodeWatchedState : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required bool Watched { get; set; }
    public required DateTime? WatchedUpdated { get; set; }
}