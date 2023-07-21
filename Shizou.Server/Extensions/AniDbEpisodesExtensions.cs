using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions;

public static class AniDbEpisodesExtensions
{
    public static AniDbEpisode? GetEpisodeByGenericFileId(this IQueryable<AniDbEpisode> queryable, IQueryable<AniDbGenericFile> genericFiles, int genericFileId)
    {
        return queryable.FirstOrDefault(e => genericFiles.Any(gf => gf.AniDbEpisodeId == e.Id && gf.Id == genericFileId));
    }
}
