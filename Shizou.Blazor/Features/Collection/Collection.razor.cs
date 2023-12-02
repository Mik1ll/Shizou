using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions.Query;

namespace Shizou.Blazor.Features.Collection;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection : IDisposable
{
    private List<AniDbAnime> _anime = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public int? FilterId { get; set; }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
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
        _anime = context.AniDbAnimes.HasLocalFiles()
            .Where(filter?.Criteria.Criterion ?? (a => true))
            .ToList();
    }

    private void OnSelectedFilterChanged(int? id)
    {
        FilterId = id;
        RefreshAnime();
    }
}
