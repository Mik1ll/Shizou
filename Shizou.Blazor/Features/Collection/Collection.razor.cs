using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Collection;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection : IDisposable
{
    private List<AniDbAnime> _anime = default!;

    private AnimeSort _sortEnum;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private AnimeService AnimeService { get; set; } = default!;

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
        _anime = AnimeService.GetFilteredAndSortedAnime(FilterId, Descending, _sortEnum, true);
    }
}
