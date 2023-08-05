using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Pages;

public partial class Anime : ComponentBase
{
    [Parameter]
    public int AnimeId { get; set; }

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    private AniDbAnime? _anime;

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.Find(AnimeId);
    }
}
