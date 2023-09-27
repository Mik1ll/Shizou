using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbEpisodesExtensions
{
    public static AniDbEpisode? ByGenericFileId(this IQueryable<AniDbEpisode> queryable, IQueryable<AniDbGenericFile> genericFiles, int genericFileId)
    {
        return queryable.SingleOrDefault(e => genericFiles.Any(gf => gf.AniDbEpisodeId == e.Id && gf.Id == genericFileId));
    }

    public static IQueryable<AniDbEpisode> WithManualLinks(this IQueryable<AniDbEpisode> queryable)
    {
        return queryable.Where(ep => ep.ManualLinkLocalFiles.Any());
    }
}
