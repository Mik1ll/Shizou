using Microsoft.AspNetCore.Components;
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
    public int AnimeId { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public string Title { get; set; } = default!;

    protected override void OnInitialized()
    {
        _posterPath = LinkGenerator.GetPathByAction(nameof(Images.GetAnimePoster), nameof(Images), new { AnimeId }) ??
                      throw new ArgumentException();
    }

    private void GoToAnime()
    {
        NavigationManager.NavigateTo($"/Collection/{AnimeId}");
    }
}
