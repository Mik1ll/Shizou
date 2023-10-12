using Microsoft.EntityFrameworkCore;
using Shizou.Data.Enums.Mal;

namespace Shizou.Data.Models;

[Owned]
public class MalStatus
{
    public required AnimeState State { get; set; }
    public required int WatchedEpisodes { get; set; }
    public required DateTime Updated { get; set; }
}
