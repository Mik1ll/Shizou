using System;
using System.ComponentModel.DataAnnotations.Schema;
using Shizou.AniDbApi.Requests.Http.Results.SubElements;
using Shizou.AniDbApi.Requests.Udp.Results;
using Shizou.Enums;

namespace Shizou.Models;

public class AniDbMyListEntry : IEntity
{
    public AniDbMyListEntry()
    {
    }

    public AniDbMyListEntry(MyListItem item)
    {
        Id = item.Id;
        Watched = item.Viewdate is not null;
        WatchedDate = item.Viewdate is null ? null : DateTime.Parse(item.Viewdate).ToUniversalTime();
        MyListState = item.State;
        MyListFileState = item.FileState;
        Updated = DateTime.SpecifyKind(DateTime.Parse(item.Updated), DateTimeKind.Utc);
    }

    public AniDbMyListEntry(AniDbFileResult result)
    {
        Id = result.MyListId!.Value;
        Watched = result.MyListViewed!.Value;
        WatchedDate = result.MyListViewDate;
        MyListState = result.MyListState!.Value;
        MyListFileState = result.MyListFileState!.Value;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public bool Watched { get; set; }
    public DateTimeOffset? WatchedDate { get; set; }
    public MyListState MyListState { get; set; }
    public MyListFileState MyListFileState { get; set; }
    public DateTimeOffset? Updated { get; set; }
}
