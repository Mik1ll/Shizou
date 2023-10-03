namespace Shizou.Data.Models;

public interface IWatchedState
{
    bool Watched { get; set; }
    DateTime? WatchedUpdated { get; set; }
    int? MyListId { get; set; }
}
