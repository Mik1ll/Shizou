using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbEpisodesExtensions
{
    public static IQueryable<AniDbEpisode> HasLocalFiles(this IQueryable<AniDbEpisode> query)
    {
        return query.Where(e => e.AniDbFiles.Any(f => f.LocalFiles.Any()));
    }
}
