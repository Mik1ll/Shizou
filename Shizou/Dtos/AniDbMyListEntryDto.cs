using System;
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Dtos
{
    public class AniDbMyListEntryDto : EntityDto
    {
        public bool Watched { get; set; }
        public DateTime? WatchedDate { get; set; }
        public MyListState MyListState { get; set; }
        public MyListFileState MyListFileState { get; set; }

        public override AniDbMyListEntry ToEntity()
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
