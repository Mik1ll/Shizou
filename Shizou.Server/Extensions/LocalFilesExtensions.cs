using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions;

public static class LocalFilesExtensions
{
    public static LocalFile? GetByEd2K(this IQueryable<LocalFile> query, string ed2K)
    {
        return query.SingleOrDefault(e => e.Ed2k == ed2K);
    }

    public static LocalFile? GetByAniDbFile(this IQueryable<LocalFile> query, AniDbFile aniDbFile)
    {
        return query.GetByEd2K(aniDbFile.Ed2k);
    }
}