using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Components.Shared;

public partial class Modal
{
    private FocusTrap? _focusTrap;
    private string _extraClasses = string.Empty;
    private string _showClass = string.Empty;
    private bool _opened = false;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    public async Task CloseAsync()
    {
        await CloseAsync(ModalResult.Ok());
    }

    public async Task CloseAsync(ModalResult modalResult)
    {
        _showClass = string.Empty;
        StateHasChanged();
        await Task.Delay(300);
        await ModalInstance.CloseAsync(modalResult);
    }

    public async Task CancelAsync()
    {
        if (OnCancel.HasDelegate)
            await OnCancel.InvokeAsync();
        await CloseAsync(ModalResult.Cancel());
    }

    public async Task CancelAsync<TPayload>(TPayload payload)
    {
        if (OnCancel.HasDelegate)
            await OnCancel.InvokeAsync();
        await CloseAsync(ModalResult.Cancel(payload));
    }

    protected override void OnParametersSet()
    {
        object? addClasses = null;
        AdditionalAttributes?.Remove("class", out addClasses);
        _extraClasses = addClasses as string ?? string.Empty;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        ModalInstance.FocusTrap = _focusTrap;
        if (!_opened)
        {
            _showClass = "show";
            _opened = true;
        }
    }
}
