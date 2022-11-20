using System;
using Microsoft.EntityFrameworkCore;
using Shizou.Enums;

namespace Shizou.Models
{
    [Owned]
    public sealed class AniDbMyListEntry
    {
        public int Id { get; set; }
        public bool Watched { get; set; }
        public DateTime? WatchedDate { get; set; }
        public MyListState MyListState { get; set; }
        public MyListFileState MyListFileState { get; set; }
    }
}
