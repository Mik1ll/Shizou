﻿using System.Linq;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class AniDbEpisodesExtensions
{
    public static AniDbEpisode? ByGenericFileId(this IQueryable<AniDbEpisode> query, ShizouContext context, int genericFileId)
    {
        return query.SingleOrDefault(e => context.AniDbGenericFiles.Any(gf => gf.AniDbEpisodeId == e.Id && gf.Id == genericFileId));
    }

    public static IQueryable<AniDbEpisode> WithManualLinks(this IQueryable<AniDbEpisode> query)
    {
        return query.Where(ep => ep.ManualLinkLocalFiles.Any());
    }
}
