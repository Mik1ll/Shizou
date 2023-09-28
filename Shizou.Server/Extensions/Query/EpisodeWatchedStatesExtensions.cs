using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class EpisodeWatchedStatesExtensions
{
    public static IQueryable<EpisodeWatchedState> EpisodeWatchedStatesWithoutMyListId(this ShizouContext context, bool excludeWithoutManualLink)
    {
        return from ws in context.EpisodeWatchedStates
            where (excludeWithoutManualLink || context.LocalFiles.Any(lf => lf.ManualLinkEpisodeId == ws.Id)) && ws.MyListId == null
            select ws;
    }
}
