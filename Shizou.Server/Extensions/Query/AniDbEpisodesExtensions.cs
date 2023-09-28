using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbEpisodesExtensions
{
    public static AniDbEpisode? AniDbEpisodeByGenericFileId(this ShizouContext context, int genericFileId)
    {
        return context.AniDbEpisodes.SingleOrDefault(e => context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == e.Id && gf.Id == genericFileId));
    }

    public static IQueryable<AniDbEpisode> AniDbEpisodesWithManualLinks(this ShizouContext context)
    {
        return context.AniDbEpisodes.Where(ep => ep.ManualLinkLocalFiles.Any());
    }
}
