using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class LocalFilesExtensions
{
    public static IQueryable<LocalFile> LocalFilesUnrecognized(this ShizouContext context)
    {
        return from lf in context.LocalFiles
            where lf.ManualLinkEpisodeId == null && !context.AniDbFiles.Any(f => f.Ed2k == lf.Ed2k)
            select lf;
    }
}
