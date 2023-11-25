using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Features.Components;

public partial class Modal
{
    private FocusTrap? _focusTrap;
    private string _classes = string.Empty;
    private string _displayClass = string.Empty;
    private bool _opened = false;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();

    public async Task CloseAsync()
    {
        await CloseAsync(ModalResult.Ok());
    }

    public async Task CloseAsync(ModalResult modalResult)
    {
        _displayClass = string.Empty;
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
        AdditionalAttributes.Remove("class", out var addClasses);
        _classes = addClasses as string ?? string.Empty;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        ModalInstance.FocusTrap = _focusTrap;
        if (!_opened)
        {
            _displayClass = "show";
            _opened = true;
        }
    }
}
