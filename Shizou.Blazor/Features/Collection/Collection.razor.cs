using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Collection;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class Collection
{
    private List<AniDbAnime> _anime = default!;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        RefreshAnime();
    }

    private void RefreshAnime()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.ToList();
    }

    private void GoToAnime(int animeId)
    {
        NavigationManager.NavigateTo($"/Collection/{animeId}");
    }
}
