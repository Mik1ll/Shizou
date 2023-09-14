using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Features.Components;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class MyAnimeListItem
{
    [Parameter]
    [EditorRequired]
    public int MalId { get; set; }

    [Parameter]
    [EditorRequired]
    public MalAnime? MalAnime { get; set; }

    [CascadingParameter]
    public ToastDisplay ToastDisplay { get; set; } = default!;

    [Inject]
    public MyAnimeListService MyAnimeListService { get; set; } = default!;


    private async Task UpdateMalStatus(MalAnime malAnime)
    {
        if (await MyAnimeListService.UpdateAnimeStatus(malAnime.Id, malAnime.Status ?? throw new InvalidOperationException()))
            ToastDisplay.AddToast("Success", "MyAnimeAnime updated successfully", ToastStyle.Success);
        else
            ToastDisplay.AddToast("Error", "MyAnimeList anime failed to update", ToastStyle.Error);
    }
}