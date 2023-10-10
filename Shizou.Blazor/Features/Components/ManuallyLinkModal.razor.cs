﻿using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class ManuallyLinkModal
{
    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    public BlazoredModalInstance ModalInstance { get; set; } = default!;

    private async Task Close()
    {
        await ModalInstance.CloseAsync();
    }

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }

    private async Task OpenAnimeModal()
    {
        var addModal = ModalService.Show<AddAnimeModal>("Add Anime");
        _ = await addModal.Result;
    }
}
