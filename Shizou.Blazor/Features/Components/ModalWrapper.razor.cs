﻿using Blazored.Modal;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class ModalWrapper
{
    private FocusTrap? _focusTrap;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    public EventCallback Cancel { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        ModalInstance.FocusTrap = _focusTrap;
    }

    private async Task CancelWrapped()
    {
        if (Cancel.HasDelegate)
            await Cancel.InvokeAsync();
        else
            await ModalInstance.CancelAsync();
    }
}
