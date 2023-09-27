using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class LocalFilesExtensions
{
    public static LocalFile? ByEd2K(this IQueryable<LocalFile> query, string ed2K)
    {
        return query.SingleOrDefault(lf => lf.Ed2k == ed2K);
    }

    public static IQueryable<LocalFile> GetUnrecognized(this IQueryable<LocalFile> query, IQueryable<AniDbFile> aniDbFiles)
    {
        return from lf in query
            where lf.ManualLinkEpisodeId == null && !aniDbFiles.Any(f => f.Ed2k == lf.Ed2k)
            select lf;
    }
}
