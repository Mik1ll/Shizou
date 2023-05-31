using ShizouCommon.Enums;

namespace ShizouContracts.Dtos;

public class AniDbMyListEntryDto : IEntityDto
{
    public int Id { get; set; }
    public required bool Watched { get; set; }
    public required DateTimeOffset? WatchedDate { get; set; }
    public required MyListState MyListState { get; set; }
    public required MyListFileState MyListFileState { get; set; }
    public DateTimeOffset? Updated { get; set; }
}
