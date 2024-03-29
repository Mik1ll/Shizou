﻿using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Components.Pages.Collection.Components;
using Shizou.Data.Database;
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
    private List<AniDbAnime> _anime = default!;
    private List<AniDbAnime>? _animeSearchResults;
    private List<AnimeFilter> _filters = default!;
    private AnimeFilter? _filter;
    private AnimeSort _sort;
    private FilterOffcanvas _filterOffcanvas = default!;
    private Dictionary<int, int>? _animeLatestLocalFileId;


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
        using var context = ContextFactory.CreateDbContext();
        _filters = context.AnimeFilters.AsNoTracking().ToList();
        _sort = Sort is null ? default : Enum.Parse<AnimeSort>(Sort);
        _filter = _filters.FirstOrDefault(f => f.Id == FilterId);
        var filtered = context.AniDbAnimes.AsNoTracking().HasLocalFiles().Where(_filter?.Criteria.Criterion ?? (_ => true)).AsEnumerable();
        IEnumerable<AniDbAnime> sorted;
        switch (_sort)
        {
            case AnimeSort.AnimeId:
                sorted = filtered.OrderBy(a => a.Id);
                break;
            case AnimeSort.AirDate:
                sorted = filtered.OrderBy(a => a.AirDate);
                break;
            case AnimeSort.Alphabetical:
                sorted = filtered.OrderBy(a => a.TitleTranscription);
                break;
            case AnimeSort.RecentFiles:
                if (_animeLatestLocalFileId is null)
                {
                    var recentlyAddedQuery = from a in context.AniDbAnimes.AsNoTracking()
                        let recentManLink = a.AniDbEpisodes.SelectMany(ep => ep.ManualLinkLocalFiles.Select(lf => lf.Id)).DefaultIfEmpty().Max()
                        let recentRegular = a.AniDbEpisodes.SelectMany(ep => ep.AniDbFiles.Select(f => f.LocalFile!.Id)).DefaultIfEmpty().Max()
                        select new { Aid = a.Id, RecentLocalId = Math.Max(recentManLink, recentRegular) };
                    _animeLatestLocalFileId = recentlyAddedQuery.ToDictionary(a => a.Aid, a => a.RecentLocalId);
                }

                sorted = from a in filtered
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
