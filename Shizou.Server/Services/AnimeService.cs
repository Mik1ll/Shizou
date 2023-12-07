using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;

namespace Shizou.Server.Services;

public enum AnimeSort
{
    AnimeId = 0,
    AirDate = 1,
    Alphabetical = 2,
    RecentFiles = 3
}

public class AnimeService
{
    private readonly IShizouContextFactory _contextFactory;

    public AnimeService(IShizouContextFactory contextFactory) => _contextFactory = contextFactory;

    public List<AniDbAnime> GetFilteredAndSortedAnime(int? filterId, bool descending, AnimeSort sort, bool hasLocalFiles)
    {
        using var context = _contextFactory.CreateDbContext();
        var filter = context.AnimeFilters.AsNoTracking().FirstOrDefault(f => f.Id == filterId);
        var query = (hasLocalFiles ? context.AniDbAnimes.HasLocalFiles() : context.AniDbAnimes).AsNoTracking()
            .Where(filter?.Criteria.Criterion ?? (a => true));
        var anime = query.ToList();
        List<AniDbAnime> sorted;
        switch (sort)
        {
            case AnimeSort.AnimeId:
                sorted = anime.OrderBy(a => a.Id).ToList();
                break;
            case AnimeSort.AirDate:
                sorted = anime.OrderBy(a => a.AirDate).ToList();
                break;
            case AnimeSort.Alphabetical:
                sorted = anime.OrderBy(a => a.TitleTranscription).ToList();
                break;
            case AnimeSort.RecentFiles:
                var recentlyAddedQuery = from a in context.AniDbAnimes.AsNoTracking()
                    let recentManLink = a.AniDbEpisodes.SelectMany(ep => ep.ManualLinkLocalFiles.Select(lf => lf.Id)).DefaultIfEmpty().Max()
                    let recentRegular = a.AniDbEpisodes.SelectMany(ep => ep.AniDbFiles.Select(f => f.LocalFile!.Id)).DefaultIfEmpty().Max()
                    select new { Aid = a.Id, RecentLocalId = Math.Max(recentManLink, recentRegular) };
                var recentlyAdded = recentlyAddedQuery.ToList();
                var orderedByRecentQuery = from a in anime
                    join ra in recentlyAdded on a.Id equals ra.Aid into rag
                    let ra = rag.FirstOrDefault()
                    orderby ra.RecentLocalId descending
                    select a;
                sorted = orderedByRecentQuery.ToList();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (descending)
            sorted.Reverse();
        return sorted;
    }
}
