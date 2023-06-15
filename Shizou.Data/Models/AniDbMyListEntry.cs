using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Common.Enums;

namespace Shizou.Data.Models;

public class AniDbMyListEntry : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required bool Watched { get; set; }
    public required DateTime? WatchedDate { get; set; }
    public required MyListState MyListState { get; set; }
    public required MyListFileState MyListFileState { get; set; }
    public DateTime? Updated { get; set; }
}
