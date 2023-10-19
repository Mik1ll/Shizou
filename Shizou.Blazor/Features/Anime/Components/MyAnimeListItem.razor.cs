using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Features.Components;
using Shizou.Data.Enums.Mal;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class MyAnimeListItem
{
    [Inject]
    private MyAnimeListService MyAnimeListService { get; set; } = default!;

    [CascadingParameter]
    private ToastDisplay ToastDisplay { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public MalAnime MalAnime { get; set; } = default!;


    private async Task UpdateStatus()
    {
        if (MalAnime.Status is null)
        {
            ToastDisplay.AddToast("Error", "Tried to update non-existant MyAnimeList status", ToastStyle.Error);
            return;
        }

        if (await MyAnimeListService.UpdateAnimeStatus(MalAnime.Id, MalAnime.Status))
            ToastDisplay.AddToast("Success", "MyAnimeList status updated successfully", ToastStyle.Success);
        else
            ToastDisplay.AddToast("Error", "MyAnimeList status failed to update", ToastStyle.Error);
    }

    private async Task AddStatus()
    {
        if (MalAnime.Status is not null)
        {
            ToastDisplay.AddToast("Error", "Tried to add MyAnimeList status when one already exists", ToastStyle.Error);
            return;
        }

        var status = new MalStatus
        {
            State = AnimeState.Watching,
            WatchedEpisodes = 0,
            Updated = DateTime.UtcNow
        };

        if (await MyAnimeListService.UpdateAnimeStatus(MalAnime.Id, status))
        {
            MalAnime.Status = status;
            ToastDisplay.AddToast("Success", "MyAnimeList status added", ToastStyle.Success);
        }
        else
        {
            ToastDisplay.AddToast("Error", "MyAnimeList status failed to add", ToastStyle.Error);
        }
    }
}
