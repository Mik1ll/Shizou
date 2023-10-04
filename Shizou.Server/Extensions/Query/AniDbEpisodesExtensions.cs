using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbEpisodesExtensions
{
    public static IQueryable<AniDbEpisode> WithManualLinks(this IQueryable<AniDbEpisode> query)
    {
        return query.Where(ep => ep.ManualLinkLocalFiles.Any());
    }
}
