using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Features.Collection.Components;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Collection;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection
{
    private List<AniDbAnime> _anime = default!;
    private List<AniDbAnime>? _animeSearchResults;
    private List<AnimeFilter> _filters = default!;
    private AnimeFilter? _filter;
    private AnimeSort _sort;
    private FilterOffcanvas _filterOffcanvas = default!;

    [Inject]
    private AnimeService AnimeService { get; set; } = default!;

    [Inject]
    private AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public int? FilterId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Sort { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public bool Descending { get; set; }

    protected override void OnParametersSet()
    {
        _sort = Sort is null ? default : Enum.Parse<AnimeSort>(Sort);
        RefreshAnime();
        RefreshFilters();
    }

    private void RefreshAnime()
    {
        _anime = AnimeService.GetFilteredAndSortedAnime(FilterId, Descending, _sort, true);
    }

    private Task<List<(int, string)>?> GetSearchResultsAsync(string query) => AnimeTitleSearchService.SearchAsync(query, true);

    private void SetSearchResults(List<(int, string)>? results)
    {
        _animeSearchResults = results is null
            ? null
            : (from res in results
                join a in _anime on res.Item1 equals a.Id
                select a).ToList();
    }


    private void OnSortSelect(ChangeEventArgs e)
    {
        NavigationManager.NavigateTo(Enum.TryParse<AnimeSort>((string)e.Value!, out var sort)
            ? NavigationManager.GetUriWithQueryParameter(nameof(Sort), (int?)sort)
            : NavigationManager.GetUriWithQueryParameter(nameof(Sort), (int?)null));
    }


    private void OnSortDirectionChanged()
    {
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Descending), !Descending));
    }


    private void RefreshFilters()
    {
        using var context = ContextFactory.CreateDbContext();
        _filters = context.AnimeFilters.ToList();
        _filter = _filters.FirstOrDefault(f => f.Id == FilterId);
    }

    private void OnFilterSelect(ChangeEventArgs e)
    {
        NavigationManager.NavigateTo(int.TryParse((string)e.Value!, out var id)
            ? NavigationManager.GetUriWithQueryParameter(nameof(FilterId), (int?)id)
            : NavigationManager.GetUriWithQueryParameter(nameof(FilterId), (int?)null));
    }

    private async Task EditFilterAsync()
    {
        await _filterOffcanvas.OpenAsync(_filter?.Id);
    }

    private async Task CreateFilterAsync()
    {
        await _filterOffcanvas.OpenAsync();
    }
}
