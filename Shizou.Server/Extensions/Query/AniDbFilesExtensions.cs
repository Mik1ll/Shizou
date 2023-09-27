using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbFilesExtensions
{
    public static AniDbFile? ByEd2K(this IQueryable<AniDbFile> query, string ed2K)
    {
        return query.SingleOrDefault(f => f.Ed2k == ed2K);
    }

    public static IQueryable<AniDbFile> WithLocal(this IQueryable<AniDbFile> query, IQueryable<LocalFile> localFiles)
    {
        return query.Where(f => localFiles.Any(lf => lf.Ed2k == f.Ed2k));
    }

    public static IQueryable<AniDbFile> ByAnimeId(this IQueryable<AniDbFile> query, IQueryable<AniDbEpisodeFileXref> epFileXrefs,
        IQueryable<AniDbEpisode> aniDbEpisodes, int animeId)
    {
        return from f in query
            join xref in epFileXrefs
                on f.Id equals xref.AniDbFileId
            join ep in aniDbEpisodes
                on xref.AniDbEpisodeId equals ep.Id
            where ep.AniDbAnimeId == animeId
            select f;
    }
}
