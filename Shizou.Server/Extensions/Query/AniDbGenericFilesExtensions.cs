using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbGenericFilesExtensions
{
    public static IQueryable<AniDbGenericFile> AniDbGenericFilesWithManualLinks(this ShizouContext context)
    {
        return from f in context.AniDbGenericFiles
            where context.AniDbEpisodesWithManualLinks().Any(ep => f.AniDbEpisodeId == ep.Id)
            select f;
    }
}
