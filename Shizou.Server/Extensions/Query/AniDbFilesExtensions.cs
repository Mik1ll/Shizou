using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbFilesExtensions
{
    public static IQueryable<AniDbNormalFile> ByAnimeId(this IQueryable<AniDbNormalFile> query, int animeId)
    {
        return from f in query
            where f.AniDbEpisodes.Any(ep => ep.AniDbAnimeId == animeId)
            select f;
    }
}
