using System;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Enums;

namespace Shizou.Models;

public class AniDbMyListEntry : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public bool Watched { get; set; }
    public DateTimeOffset? WatchedDate { get; set; }
    public MyListState MyListState { get; set; }
    public MyListFileState MyListFileState { get; set; }
    public DateTimeOffset? Updated { get; set; }
}