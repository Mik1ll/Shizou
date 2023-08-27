using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(Ed2k), IsUnique = true)]
[Index(nameof(MyListId))]
public class FileWatchedState : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public required string Ed2k { get; set; } = default!;

    public required bool Watched { get; set; }
    public required DateTime? WatchedUpdated { get; set; }
    public int? MyListId { get; set; }
}