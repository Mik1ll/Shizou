using Microsoft.AspNetCore.Components;
using Shizou.Data.Models;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Components.Shared;

public partial class AnimeCard
{
    private string _posterPath = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public AniDbAnime AniDbAnime { get; set; } = default!;

    protected override void OnInitialized()
    {
        _posterPath = LinkGenerator.GetPathByAction(nameof(Images.GetAnimePoster), nameof(Images), new { AnimeId = AniDbAnime.Id }) ??
                      throw new ArgumentException();
    }


    private void GoToAnime(int animeId)
    {
        NavigationManager.NavigateTo($"/Collection/{animeId}", true);
    }
}
