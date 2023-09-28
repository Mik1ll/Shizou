using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class FileWatchedStatesExtensions
{
    public static IQueryable<FileWatchedState> FileWatchedStatesWithoutMyListId(this ShizouContext context, bool excludeWithoutLocal)
    {
        return from ws in context.FileWatchedStates
            where (excludeWithoutLocal || context.LocalFiles.Any(lf => ws.Ed2k == lf.Ed2k)) && ws.MyListId == null
            select ws;
    }
}
