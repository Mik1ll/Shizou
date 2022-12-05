using System.Linq;
using Shizou.Models;

namespace Shizou.Extensions;

public static class AniDbFilesExtensions
{
    public static AniDbFile? GetByEd2K(this IQueryable<AniDbFile> query, string ed2K)
    {
        return query.FirstOrDefault(e => e.Ed2K == ed2K);
    }

    public static AniDbFile? GetByLocalFile(this IQueryable<AniDbFile> query, LocalFile localFile)
    {
        return query.GetByEd2K(localFile.Ed2K);
    }
}