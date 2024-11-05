using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Components.Pages.Collection.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

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
    private List<AniDbAnime>? _anime;
    private List<AniDbAnime>? _animeSearchResults;
    private List<AnimeFilter> _filters = default!;
    private AnimeFilter? _filter;
    private FilterOffcanvas _filterOffcanvas = default!;
    private HashSet<int>? _aids;
    private AnimeSort SortEnum => (AnimeSort)Sort;
    private AnimeSeason? SeasonEnum => (AnimeSeason?)Season;
    private AnimeType? AnimeTypeEnum => (AnimeType?)AnimeType;
    private LiveSearchBox? _searchBox;

    [Inject]
    private IAnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

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

    [Parameter]
    [SupplyParameterFromQuery]
    public int? Season { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public int? Year { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public int? AnimeType { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var context = ContextFactory.CreateDbContext();
        _filters = context.AnimeFilters.AsNoTracking().ToList();
        _filter = _filters.FirstOrDefault(f => f.Id == FilterId);
        var queryable = context.AniDbAnimes.AsNoTracking().HasLocalFiles();
        if (_filter is not null)
            queryable = queryable.Where(_filter.Criteria.Criterion);
        FilterSeasonYear(ref queryable);
        if (AnimeTypeEnum is not null)
            queryable = queryable.Where(a => a.AnimeType == AnimeTypeEnum);
        SortAnime(ref queryable);
        _anime = queryable.ToList();
        _aids = _anime?.Select(a => a.Id).ToHashSet();
        if (!string.IsNullOrWhiteSpace(_searchBox?.Query))
            SetSearchResults(await GetSearchResultsAsync(_searchBox.Query));
    }

    private void FilterSeasonYear(ref IQueryable<AniDbAnime> queryable)
    {
        if (Year is null && SeasonEnum is null)
            return;
        var criteria = new List<TermCriterion>();
        if (Year is not null)
            criteria.AddRange([
                new AirDateCriterion(false, AirDateTermType.AirDate, AirDateTermRange.OnOrAfter, Year),
                new AirDateCriterion(false, AirDateTermType.AirDate, AirDateTermRange.Before, Year + 1)
            ]);
        if (SeasonEnum is not null)
            criteria.Add(new SeasonCriterion(false, SeasonEnum.Value));
        queryable = queryable.Where(new AndAllCriterion(criteria).Criterion);
    }

    private void SortAnime(ref IQueryable<AniDbAnime> queryable)
    {
        switch (SortEnum)
        {
            case AnimeSort.AnimeId:
                queryable = queryable.OrderBy(a => a.Id);
                break;
            case AnimeSort.AirDate:
                queryable = queryable.OrderBy(a => a.AirDate);
                break;
            case AnimeSort.Alphabetical:
                queryable = queryable.OrderBy(a => a.TitleTranscription);
                break;
            case AnimeSort.RecentFiles:
            {
                queryable = queryable.OrderByDescending(a =>
                    a.AniDbEpisodes.SelectMany(e => e.AniDbFiles.SelectMany(f => f.LocalFiles.Select(lf => lf.Id)))
                        .DefaultIfEmpty().Max());
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        queryable = Descending ? queryable.Reverse() : queryable;
    }

    private Task<List<(int, string)>?> GetSearchResultsAsync(string query) => AnimeTitleSearchService.SearchAsync(query, _aids);

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

    private void OnSeasonSelect(ChangeEventArgs e)
    {
        var season = Enum.TryParse<AnimeSeason>((string?)e.Value, out var outSeason) ? (AnimeSeason?)outSeason : null;
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Season), (int?)season));
    }

    private void OnAnimeTypeSelect(ChangeEventArgs e)
    {
        var animeType = Enum.TryParse<AnimeType>((string?)e.Value, out var outAnimeType) ? (AnimeType?)outAnimeType : null;
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(AnimeType), (int?)animeType));
    }

    private void OnSortDirectionChanged()
    {
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Descending), !Descending));
    }

    private void OnYearChanged()
    {
        if (Year <= 1900)
            Year = DateTimeOffset.UtcNow.Year;
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Year), Year));
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
