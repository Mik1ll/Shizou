using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class EpisodeWatchedStatesExtensions
{
    public static IQueryable<EpisodeWatchedState> WithManualLinks(this IQueryable<EpisodeWatchedState> query, ShizouContext context)
    {
        return from ws in query
            where context.LocalFiles.Any(lf => lf.ManualLinkEpisodeId == ws.Id)
            select ws;
    }
}
