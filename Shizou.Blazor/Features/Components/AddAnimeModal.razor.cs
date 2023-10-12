﻿using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Components;

public partial class AddAnimeModal
{
    private int? _selected;

    [Inject]
    private AnimeTitleSearchService AnimeTitleSearchService { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [CascadingParameter]
    private ToastDisplay ToastDisplay { get; set; } = default!;


    private async Task<List<(int, string)>?> GetTitles(string query)
    {
        return (await AnimeTitleSearchService.Search(query))?.Select(p => (p.Item1, $"{p.Item1} {p.Item2}")).ToList();
    }

    private async Task AddAnime()
    {
        if (_selected is null)
        {
            ToastDisplay.AddToast("Add anime failed", "No anime to add!", ToastStyle.Error);
        }
        else
        {
            CommandService.Dispatch(new AnimeArgs(_selected.Value));
            ToastDisplay.AddToast($"Adding anime {_selected}", "You may need to wait for the anime to be processed before it is available", ToastStyle.Success);
        }

        await ModalInstance.CloseAsync();
    }

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }
}