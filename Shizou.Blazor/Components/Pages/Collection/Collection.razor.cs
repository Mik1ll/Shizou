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
    private int? _oldSeason;
    private int? _oldYear;
    private int _oldAnimeType;
    private AnimeSort SortEnum => (AnimeSort)Sort;
    private AnimeSeason? SeasonEnum => (AnimeSeason?)Season;
    private AnimeType AnimeTypeEnum => (AnimeType)AnimeType;
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
    public int AnimeType { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var context = ContextFactory.CreateDbContext();
        _filters = context.AnimeFilters.AsNoTracking().ToList();
        var newFilter = _filters.FirstOrDefault(f => f.Id == FilterId);
        if (_anime is null || newFilter?.Criteria != _filter?.Criteria || Year != _oldYear || Season != _oldSeason || AnimeType != _oldAnimeType)
            _anime = context.AniDbAnimes.AsNoTracking().HasLocalFiles().Where(newFilter?.Criteria.Criterion ?? (_ => true)).ToList();
        _filter = newFilter;
        _oldYear = Year;
        _oldSeason = Season;
        _oldAnimeType = AnimeType;
        FilterSeasonYear();
        SortAnime();
        _anime = _anime.Where(a => AnimeType == 0 || a.AnimeType == AnimeTypeEnum).ToList();
        _aids = _anime?.Select(a => a.Id).ToHashSet();
        if (!string.IsNullOrWhiteSpace(_searchBox?.Query))
            SetSearchResults(await GetSearchResultsAsync(_searchBox.Query));
    }

    private void FilterSeasonYear()
    {
        if (_anime is null || Year is null || SeasonEnum is null)
            return;

        var crit = new SeasonCriterion(false, SeasonEnum.Value);
        _anime = _anime.Where(crit.Criterion.Compile()).ToList();
    }

    private void SortAnime()
    {
        if (_anime is null)
            return;
        IEnumerable<AniDbAnime> sorted;
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
            {
                using var context = ContextFactory.CreateDbContext();
                sorted = context.AniDbAnimes.AsNoTracking().OrderByDescending(a =>
                    a.AniDbEpisodes.SelectMany(e => e.AniDbFiles.SelectMany(f => f.LocalFiles.Select(lf => lf.Id)))
                        .DefaultIfEmpty().Max()).ToList();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        _anime = (Descending ? sorted.Reverse() : sorted).ToList();
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
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>()
        {
            { nameof(Season), (int?)season },
            { nameof(Year), Year ?? DateTimeOffset.UtcNow.Year }
        }));
    }

    private void OnAnimeTypeSelect(ChangeEventArgs e)
    {
        Enum.TryParse<AnimeType>((string?)e.Value, out var animeType);
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(AnimeType), (int)animeType));
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
