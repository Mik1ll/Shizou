using System;
using Microsoft.EntityFrameworkCore;
using Shizou.Dtos;
using Shizou.Enums;

namespace Shizou.Entities
{
    [Owned]
    public class AniDbMyListEntry : Entity
    {
        public bool Watched { get; set; }
        public DateTime? WatchedDate { get; set; }
        public MyListState MyListState { get; set; }
        public MyListFileState MyListFileState { get; set; }

        public override AniDbMyListEntryDto ToDto()
        {
            return new()
            {
                Id = Id,
                Watched = Watched,
                WatchedDate = WatchedDate,
                MyListState = MyListState,
                MyListFileState = MyListFileState
            };
        }
    }
}
