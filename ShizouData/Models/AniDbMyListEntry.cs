using System.ComponentModel.DataAnnotations.Schema;
using ShizouData.Enums;

namespace ShizouData.Models;

public class AniDbMyListEntry : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required bool Watched { get; set; }
    public required DateTimeOffset? WatchedDate { get; set; }
    public required MyListState MyListState { get; set; }
    public required MyListFileState MyListFileState { get; set; }
    public DateTimeOffset? Updated { get; set; }
}
