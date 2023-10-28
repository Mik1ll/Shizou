using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbAnimesExtensions
{
    public static IQueryable<AniDbAnime> HasLocalFiles(this IQueryable<AniDbAnime> query)
    {
        return query.Where(a => a.AniDbEpisodes.Any(e => e.AniDbFiles.Any(f => f.LocalFile != null) || e.ManualLinkLocalFiles.Any()));
    }
}
