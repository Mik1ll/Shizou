using System.Linq;
using ShizouData.Models;

namespace Shizou.Extensions;

public static class LocalFilesExtensions
{
    public static LocalFile? GetByEd2K(this IQueryable<LocalFile> query, string ed2K)
    {
        return query.SingleOrDefault(e => e.Ed2K == ed2K);
    }

    public static LocalFile? GetByAniDbFile(this IQueryable<LocalFile> query, AniDbFile aniDbFile)
    {
        return query.GetByEd2K(aniDbFile.Ed2K);
    }
}