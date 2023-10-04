using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(MyListId))]
public class FileWatchedState : IWatchedState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int AniDbFileId { get; set; }

    public AniDbFile AniDbFile { get; set; } = default!;

    public required bool Watched { get; set; }
    public required DateTime? WatchedUpdated { get; set; }
    public int? MyListId { get; set; }
}
