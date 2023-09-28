using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbFilesExtensions
{
    public static IQueryable<AniDbFile> AniDbFilesWithLocalFile(this ShizouContext context)
    {
        return context.AniDbFiles.Where(f => context.LocalFiles.Any(lf => lf.Ed2k == f.Ed2k));
    }

    public static IQueryable<AniDbFile> AniDbFilesByAnimeId(this ShizouContext context, int animeId)
    {
        return from f in context.AniDbFiles
            join xref in context.AniDbEpisodeFileXrefs
                on f.Id equals xref.AniDbFileId
            join ep in context.AniDbEpisodes
                on xref.AniDbEpisodeId equals ep.Id
            where ep.AniDbAnimeId == animeId
            select f;
    }
}
