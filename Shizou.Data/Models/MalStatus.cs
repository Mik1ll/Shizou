using Microsoft.EntityFrameworkCore;
using Shizou.Data.Enums.Mal;

namespace Shizou.Data.Models;

[Owned]
public class MalStatus
{
    public AnimeState State { get; set; }
    public int WatchedEpisodes { get; set; }
    public DateTime Updated { get; set; }
}