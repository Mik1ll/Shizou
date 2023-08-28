using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(MyListId))]
public class EpisodeWatchedState : IWatchedState
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required bool Watched { get; set; }
    public required DateTime? WatchedUpdated { get; set; }
    public int? MyListId { get; set; }
}