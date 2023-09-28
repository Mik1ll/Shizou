using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbGenericFilesExtensions
{
    public static IQueryable<AniDbGenericFile> WithManualLinks(this IQueryable<AniDbGenericFile> query, ShizouContext context)
    {
        return from f in query
            where context.AniDbEpisodes.WithManualLinks().Any(ep => f.AniDbEpisodeId == ep.Id)
            select f;
    }
}
