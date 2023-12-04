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
                var updateAidsQuery = from updateAid in (from lf in context.LocalFiles
                        where lf.AniDbFile != null
                        from aniDbAnimeId in lf.AniDbFile.AniDbEpisodes.Select(e => e.AniDbAnimeId)
                        select new { lf.Updated, Aid = aniDbAnimeId }).Union(from lf in context.LocalFiles
                        where lf.ManualLinkEpisode != null
                        select new { lf.Updated, Aid = lf.ManualLinkEpisode.AniDbAnimeId })
                    group updateAid by updateAid.Aid
                    into grp
                    select (from updateAid in grp
                        orderby updateAid.Updated descending
                        select updateAid).First();
                var updateAids = updateAidsQuery.AsNoTracking().ToList();
                var resquery = from a in anime
                    join ua in updateAids on a.Id equals ua.Aid into uas
                    let ua = uas.FirstOrDefault()
                    orderby ua.Updated descending
                    select a;
                sorted = resquery.ToList();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (descending)
            sorted.Reverse();
        return sorted;
    }
}
