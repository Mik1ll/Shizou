using Microsoft.AspNetCore.Components;
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
            ToastService.ShowError("Error", "Tried to update non-existant MyAnimeList status");
            return;
        }

        if (await MyAnimeListService.UpdateAnimeStatusAsync(MalAnime.Id, MalAnime.Status))
            ToastService.ShowSuccess("Success", "MyAnimeList status updated successfully");
        else
            ToastService.ShowError("Error", "MyAnimeList status failed to update");
    }

    private async Task AddStatusAsync()
    {
        if (MalAnime.Status is not null)
        {
            ToastService.ShowError("Error", "Tried to add MyAnimeList status when one already exists");
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
            ToastService.ShowSuccess("Success", "MyAnimeList status added");
        }
        else
        {
            ToastService.ShowError("Error", "MyAnimeList status failed to add");
        }
    }
}
