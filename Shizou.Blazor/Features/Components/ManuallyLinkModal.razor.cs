﻿using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class ManuallyLinkModal
{
    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    private async Task Cancel()
    {
        await ModalInstance.CancelAsync();
    }

    private async Task OpenAnimeModal()
    {
        await ModalService.Show<AddAnimeModal>().Result;
    }
}