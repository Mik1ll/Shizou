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
        switch (_sortEnum)
        {
            case AnimeSort.AnimeId:
                query = query.OrderBy(a => a.Id);
                break;
            case AnimeSort.AirDate:
                query = query.OrderBy(a => a.AirDate);
                break;
            case AnimeSort.Alphabetical:
                query = query.OrderBy(a => a.TitleTranscription);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var result = query.ToList();
        if (Descending)
            result.Reverse();
        _anime = result;
    }
}
