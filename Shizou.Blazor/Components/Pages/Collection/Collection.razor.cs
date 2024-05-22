using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Components.Pages.Collection.Components;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;
using AnimeResult = (int Id, string? AirDate, string TitleTranscription);

namespace Shizou.Blazor.Components.Pages.Collection;

public enum AnimeSort
{
    AnimeId = 0,
    AirDate = 1,
    Alphabetical = 2,
    RecentFiles = 3
}

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection
{
    private List<AnimeResult>? _anime;
    private List<AnimeResult>? _animeSearchResults;
    private List<AnimeFilter> _filters = default!;
    private AnimeFilter? _filter;
    private FilterOffcanvas _filterOffcanvas = default!;
    private Dictionary<int, int>? _animeLatestLocalFileId;
    private AnimeSort SortEnum => (AnimeSort)Sort;

    [Inject]
    private AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public int FilterId { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public int Sort { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public bool Descending { get; set; }

    protected override void OnParametersSet()
    {
        Load();
    }

    private void Load()
    {
        using var context = ContextFactory.CreateDbContext();
        _filters = context.AnimeFilters.AsNoTracking().ToList();
        var newFilter = _filters.FirstOrDefault(f => f.Id == FilterId);
        if (_anime is null || newFilter?.Criteria != _filter?.Criteria)
            _anime = context.AniDbAnimes.AsNoTracking().HasLocalFiles().Where(newFilter?.Criteria.Criterion ?? (_ => true))
                .Select(a => new { a.Id, a.AirDate, a.TitleTranscription }).AsEnumerable()
                .Select(a => (a.Id, a.AirDate, a.TitleTranscription)).ToList();
        _filter = newFilter;
        SortAnime();
    }

    private void SortAnime()
    {
        if (_anime is null)
            return;
        IEnumerable<AnimeResult> sorted;
        switch (SortEnum)
        {
            case AnimeSort.AnimeId:
                sorted = _anime.OrderBy(a => a.Id);
                break;
            case AnimeSort.AirDate:
                sorted = _anime.OrderBy(a => a.AirDate);
                break;
            case AnimeSort.Alphabetical:
                sorted = _anime.OrderBy(a => a.TitleTranscription);
                break;
            case AnimeSort.RecentFiles:
                if (_animeLatestLocalFileId is null)
                {
                    using var context = ContextFactory.CreateDbContext();
                    var recentlyAddedQuery = from a in context.AniDbAnimes.AsNoTracking()
                        let recentRegular = a.AniDbEpisodes.SelectMany(ep => ep.AniDbFiles.SelectMany(f => f.LocalFiles).Select(f => f.Id)).DefaultIfEmpty()
                            .Max()
                        select new { Aid = a.Id, RecentLocalId = recentRegular };
                    _animeLatestLocalFileId = recentlyAddedQuery.ToDictionary(a => a.Aid, a => a.RecentLocalId);
                }

                sorted = from a in _anime
                    orderby _animeLatestLocalFileId.GetValueOrDefault(a.Id, 0) descending
                    select a;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _anime = (Descending ? sorted.Reverse() : sorted).ToList();
    }

    private Task<List<(int, string)>?> GetSearchResultsAsync(string query) => AnimeTitleSearchService.SearchAsync(query, true);

    private void SetSearchResults(List<(int, string)>? results)
    {
        _animeSearchResults = results is null || _anime is null
            ? null
            : (from res in results
                join a in _anime on res.Item1 equals a.Id
                select a).ToList();
    }

    private void OnSortSelect(ChangeEventArgs e)
    {
        Enum.TryParse<AnimeSort>((string?)e.Value, out var sort);
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Sort), (int)sort));
    }

    private void OnSortDirectionChanged()
    {
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Descending), !Descending));
    }

    private void OnFilterSelect(ChangeEventArgs e)
    {
        int.TryParse((string?)e.Value, out var filterId);
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(FilterId), filterId));
    }

    private async Task EditFilterAsync()
    {
        if (_filter is not null)
            await _filterOffcanvas.OpenEditAsync(_filter);
    }

    private async Task CreateFilterAsync()
    {
        await _filterOffcanvas.OpenNewAsync();
    }
}
