using Microsoft.AspNetCore.Components;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Components;

public partial class AnimeCard
{
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public AniDbAnime AniDbAnime { get; set; } = default!;


    private void GoToAnime(int animeId)
    {
        NavigationManager.NavigateTo($"/Collection/{animeId}", true);
    }
}
