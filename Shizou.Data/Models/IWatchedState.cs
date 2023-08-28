namespace Shizou.Data.Models;

public interface IWatchedState : IEntity
{
    bool Watched { get; set; }
    DateTime? WatchedUpdated { get; set; }
    int? MyListId { get; set; }
}