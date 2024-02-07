using Microsoft.AspNetCore.Components;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Collection;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection
{
    private List<AniDbAnime> _anime = default!;
    private List<AniDbAnime>? _animeSearchResults;
    private AnimeSort _sort;

    [Inject]
    private AnimeService AnimeService { get; set; } = default!;

    [Inject]
    private AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

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

    protected override void OnParametersSet()
    {
        _sort = Sort is null ? default : Enum.Parse<AnimeSort>(Sort);
        RefreshAnime();
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
        if (Enum.TryParse<AnimeSort>((string)e.Value!, out var sort))
            NavigateToSort(sort);
        else
            NavigateToSort(null);
    }

    private void NavigateToSort(AnimeSort? sort)
    {
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Sort), (int?)sort));
    }


    private void OnSortDirectionChanged()
    {
        NavigateToSortDirection(!Descending);
    }

    private void NavigateToSortDirection(bool descending)
    {
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Descending), descending));
    }
}
