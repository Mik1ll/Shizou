using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Features.Components;
using Shizou.Blazor.Services;
using Shizou.Data.Enums.Mal;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class MyAnimeListItem
{
    [Inject]
    private MyAnimeListService MyAnimeListService { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public MalAnime MalAnime { get; set; } = default!;


    private async Task UpdateStatusAsync()
    {
        if (MalAnime.Status is null)
        {
            ToastService.AddToast("Error", "Tried to update non-existant MyAnimeList status", ToastStyle.Error);
            return;
        }

        if (await MyAnimeListService.UpdateAnimeStatusAsync(MalAnime.Id, MalAnime.Status))
            ToastService.AddToast("Success", "MyAnimeList status updated successfully", ToastStyle.Success);
        else
            ToastService.AddToast("Error", "MyAnimeList status failed to update", ToastStyle.Error);
    }

    private async Task AddStatusAsync()
    {
        if (MalAnime.Status is not null)
        {
            ToastService.AddToast("Error", "Tried to add MyAnimeList status when one already exists", ToastStyle.Error);
            return;
        }

        var status = new MalStatus
        {
            State = AnimeState.Watching,
            WatchedEpisodes = 0,
            Updated = DateTime.UtcNow
        };

        if (await MyAnimeListService.UpdateAnimeStatusAsync(MalAnime.Id, status))
        {
            MalAnime.Status = status;
            ToastService.AddToast("Success", "MyAnimeList status added", ToastStyle.Success);
        }
        else
        {
            ToastService.AddToast("Error", "MyAnimeList status failed to add", ToastStyle.Error);
        }
    }
}
