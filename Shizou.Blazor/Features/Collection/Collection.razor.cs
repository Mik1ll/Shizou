using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;

namespace Shizou.Blazor.Features.Collection;

public enum AnimeSort
{
    AnimeId = 0,
    AirDate = 1,
    Alphabetical = 2,
    RecentFiles = 3
}

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection : IDisposable
{
    private List<AniDbAnime> _anime = default!;

    private AnimeSort _sortEnum;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public int? FilterId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Sort { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public bool Descending { get; set; }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    protected override void OnParametersSet()
    {
        _sortEnum = Sort is null ? default : Enum.Parse<AnimeSort>(Sort);
    }

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        RefreshAnime();
    }

    private void OnLocationChanged(object? o, LocationChangedEventArgs locationChangedEventArgs)
    {
        RefreshAnime();
        StateHasChanged();
    }


    private void RefreshAnime()
    {
        using var context = ContextFactory.CreateDbContext();
        var filter = context.AnimeFilters.FirstOrDefault(f => f.Id == FilterId);
        var query = context.AniDbAnimes.HasLocalFiles()
            .Where(filter?.Criteria.Criterion ?? (a => true));
        var anime = query.ToList();
        List<AniDbAnime> sorted;
        switch (_sortEnum)
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
                var updateAids = updateAidsQuery.ToList();
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

        if (Descending)
            sorted.Reverse();
        _anime = sorted;
    }
}
