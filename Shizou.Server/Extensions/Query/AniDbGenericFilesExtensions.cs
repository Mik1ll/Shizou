using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbGenericFilesExtensions
{
    public static IQueryable<AniDbGenericFile> WithManualLinks(this IQueryable<AniDbGenericFile> query, IQueryable<AniDbEpisode> aniDbEpisodes)
    {
        return from f in query
            where aniDbEpisodes.WithManualLinks().Any(ep => f.AniDbEpisodeId == ep.Id)
            select f;
    }
}
