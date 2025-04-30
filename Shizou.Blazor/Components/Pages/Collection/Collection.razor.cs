using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Components.Pages.Collection.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Collection;

public enum AnimeSort
{
    RecentlyAdded = 0,
    AnimeId,
    Alphabetical,
    AirDate,
    EpisodeAirDate,
    RecentlyWatched,
}

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection
{
    private List<AnimeQueryResult>? _anime;
    private List<AnimeQueryResult>? _animeSearchResults;
    private List<AnimeFilter> _filters = null!;
    private AnimeFilter? _filter;
    private FilterOffcanvas _filterOffcanvas = null!;
    private HashSet<int>? _aids;
    private AnimeSort SortEnum => (AnimeSort)Sort;
    private AnimeSeason? SeasonEnum => (AnimeSeason?)Season;
    private AnimeType? AnimeTypeEnum => (AnimeType?)AnimeType;
    private LiveSearchBox? _searchBox;

    [Inject]
    private IAnimeTitleSearchService AnimeTitleSearchService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = null!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

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
        _anime = queryable.Select(a => new AnimeQueryResult(a.Id, a.TitleTranscription)).ToList();
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
                new AirDateCriterion { AirDateTermType = AirDateTermType.AirDate, AirDateTermRange = AirDateTermRange.OnOrAfter, Year = Year },
                new AirDateCriterion { AirDateTermType = AirDateTermType.AirDate, AirDateTermRange = AirDateTermRange.Before, Year = Year + 1 },
            ]);
        if (SeasonEnum is not null)
            criteria.Add(new SeasonCriterion { Season = SeasonEnum.Value });
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
            case AnimeSort.RecentlyAdded:
                queryable = queryable.OrderByDescending(a =>
                    a.AniDbEpisodes.SelectMany(e => e.AniDbFiles.SelectMany(f => f.LocalFiles.Select(lf => lf.Id)))
                        .DefaultIfEmpty().Max());
                break;
            case AnimeSort.RecentlyWatched:
                queryable = queryable.OrderByDescending(a =>
                    a.AniDbEpisodes.SelectMany(e =>
                        e.AniDbFiles.Where(f => f.FileWatchedState.Watched).Select(f =>
                            f.FileWatchedState.WatchedUpdated
                        )
                    ).DefaultIfEmpty().Max()
                );
                break;
            case AnimeSort.EpisodeAirDate:
                queryable = queryable.OrderByDescending(a =>
                    a.AniDbEpisodes.Where(e => e.AniDbFiles.Any(f => f.LocalFiles.Count != 0)).Select(e => e.AirDate).DefaultIfEmpty().Max()
                );
                break;
            default:
                throw new IndexOutOfRangeException(nameof(Sort));
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
        var sort = Enum.TryParse<AnimeSort>((string?)e.Value, out var outSort) ? (int?)outSort : null;
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Sort), sort));
    }

    private void OnSeasonSelect(ChangeEventArgs e)
    {
        var season = Enum.TryParse<AnimeSeason>((string?)e.Value, out var outSeason) ? (int?)outSeason : null;
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(Season), season));
    }

    private void OnAnimeTypeSelect(ChangeEventArgs e)
    {
        var animeType = Enum.TryParse<AnimeType>((string?)e.Value, out var outAnimeType) ? (int?)outAnimeType : null;
        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter(nameof(AnimeType), animeType));
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
        var filterId = int.TryParse((string?)e.Value, out var outFilterId) ? (int?)outFilterId : null;
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

    private string GetPosterPath(AnimeQueryResult anime) =>
        LinkGenerator.GetPathByAction(nameof(Images.GetAnimePoster), nameof(Images), new { animeId = anime.Id }) ??
        throw new ArgumentException("Could not generate anime poster path");

    private record AnimeQueryResult(int Id, string TitleTranscription);
}
