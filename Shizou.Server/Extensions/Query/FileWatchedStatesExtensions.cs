using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class FileWatchedStatesExtensions
{
    public static IQueryable<FileWatchedState> WithLocalFile(this IQueryable<FileWatchedState> query, ShizouContext context)
    {
        return from ws in query
            where context.LocalFiles.Any(lf => ws.Ed2k == lf.Ed2k)
            select ws;
    }
}
