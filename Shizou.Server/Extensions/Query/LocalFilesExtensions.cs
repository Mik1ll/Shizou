using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class LocalFilesExtensions
{
    public static IQueryable<LocalFile> Unrecognized(this IQueryable<LocalFile> query, ShizouContext context)
    {
        return from lf in query
            where lf.ManualLinkEpisodeId == null && lf.AniDbFile == null
            select lf;
    }
}
