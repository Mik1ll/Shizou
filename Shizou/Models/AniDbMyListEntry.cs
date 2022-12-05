using System;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.Enums;

namespace Shizou.Models;

public sealed class AniDbMyListEntry : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public bool Watched { get; set; }
    public DateTime? WatchedDate { get; set; }
    public MyListState MyListState { get; set; }
    public MyListFileState MyListFileState { get; set; }
}